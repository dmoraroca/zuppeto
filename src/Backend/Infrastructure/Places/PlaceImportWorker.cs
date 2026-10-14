using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places;

internal sealed class PlaceImportWorker(IServiceScopeFactory scopes, ILogger<PlaceImportWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Heartbeat = TimeSpan.FromSeconds(15);
    private readonly string owner=$"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try { var work=await Claim(stoppingToken); if(work is null){await Task.Delay(1000,stoppingToken);continue;} await Process(work,stoppingToken); }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogError(ex,"Error no associat a cap importació de Places.");await Task.Delay(1000,stoppingToken);}
        }
    }
    private async Task<PlaceImportWorkItem?> Claim(CancellationToken ct){await using var s=scopes.CreateAsyncScope();return await s.ServiceProvider.GetRequiredService<IPlaceImportWorkQueue>().ClaimNextAsync(owner,Lease,ct);}
    private async Task Process(PlaceImportWorkItem work,CancellationToken ct)
    {
        using var heartbeatCts=CancellationTokenSource.CreateLinkedTokenSource(ct);var heartbeat=Maintain(work.ImportId,heartbeatCts.Token);
        try
        {
            await using var s=scopes.CreateAsyncScope(); var providers=s.ServiceProvider.GetServices<IPlaceDiscoverySource>();
            var repository=s.ServiceProvider.GetRequiredService<IPlaceImportRepository>();
            if(await CompleteCancellationIfRequested(repository,work,ct))return;
            var source=providers.Single(x=>x.Descriptor.SourceKey==work.SourceKey&&x.Descriptor.AccessMode==work.AccessMode); var discovery=await source.DiscoverAsync(work.Context,ct);
            if(!discovery.IsSuccess) throw new PlaceSourceException(discovery.Failure!);
            if(await CompleteCancellationIfRequested(repository,work,ct))return;
            var normalizer=s.ServiceProvider.GetRequiredService<IPlaceSourceNormalizer>();var matcher=s.ServiceProvider.GetRequiredService<IPlaceMatcher>();
            var storagePolicy=s.ServiceProvider.GetRequiredService<IPlaceStoragePermissionPolicy>();
            var enrichers=s.ServiceProvider.GetServices<IPlaceEnrichmentSource>().ToArray();
            var staged=new List<PlaceImportStagedItem>();var issues=new List<PlaceImportIssueDto>();
            foreach(var document in discovery.Value!.Documents)
            {
                if(await CompleteCancellationIfRequested(repository,work,ct))return;
                var denied=document.Fields.Keys.Select(field=>storagePolicy.Evaluate(source.Descriptor,field))
                    .FirstOrDefault(result=>!result.IsSuccess||result.Value?.Allowed!=true);
                if(denied is not null)
                {
                    issues.Add(new(Guid.NewGuid(),"STORAGE_NOT_ALLOWED","Error",denied.Failure?.Message??denied.Value?.Reason??"La persistència del camp no està permesa.",document.ExternalId));
                    continue;
                }
                var normalized=normalizer.Normalize(document);
                if(!normalized.IsSuccess){issues.Add(new(Guid.NewGuid(),"NORMALIZATION", "Warning",normalized.Failure!.Message,document.ExternalId));continue;}
                var candidate=normalized.Value! with { CountryId=work.Context.CountryId,TerritorialUnitId=work.Context.TerritorialUnitId };
                var enrichments=new List<PlaceSourceDocument>();
                if(candidate.Fields.TryGetValue("wikidata.id",out var wikidataId))
                {
                    var enricher=enrichers.SingleOrDefault(x=>x.Descriptor.SourceKey==WikidataSource.SourceKey);
                    if(enricher is not null)
                    {
                        var enrichment=await enricher.EnrichAsync(wikidataId.NormalizedValue,ct);
                        if(enrichment.IsSuccess)
                        {
                            enrichments.Add(enrichment.Value!);
                            var merged=candidate.Fields.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.OrdinalIgnoreCase);
                            foreach(var field in enrichment.Value!.Fields)merged.TryAdd(field.Key,field.Value);
                            candidate=candidate with{Fields=merged};
                        }
                        else issues.Add(new(Guid.NewGuid(),"WIKIDATA_ENRICHMENT","Warning",enrichment.Failure!.Message,document.ExternalId));
                    }
                }
                var match=await matcher.MatchAsync(candidate,ct); if(!match.IsSuccess){issues.Add(new(Guid.NewGuid(),"MATCH", "Warning",match.Failure!.Message,document.ExternalId));continue;}
                var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',candidate.Fields.OrderBy(x=>x.Key).Select(x=>$"{x.Key}={x.Value.NormalizedValue}"))))).ToLowerInvariant();
                staged.Add(new(document,candidate,match.Value!,hash,enrichments));
            }
            await repository.StageAsync(work.ImportId,discovery.Value!,staged,issues,ct);
            await s.ServiceProvider.GetRequiredService<IPlaceImportWorkQueue>().CompleteAsync(work.ImportId,owner,ct);
        }
        catch(Exception ex) when(!ct.IsCancellationRequested)
        {
            var (code,message,recoverable)=ex switch { PlaceSourceException p=>(p.Failure.Kind.ToString().ToUpperInvariant(),p.Failure.Message,p.Failure.Kind is FailureKind.ExternalUnavailable or FailureKind.RateLimited), _=>("PLACE_IMPORT_FAILED","La importació de Places ha fallat.",false)};
            await WithQueue(q=>q.FailAsync(work.ImportId,owner,code,message,recoverable,3,ct));logger.LogError(ex,"Ha fallat la importació de Places {ImportId}.",work.ImportId);
        }
        finally{heartbeatCts.Cancel();try{await heartbeat;}catch(OperationCanceledException){}}
    }
    private async Task Maintain(Guid id,CancellationToken ct){while(!ct.IsCancellationRequested){await Task.Delay(Heartbeat,ct);await WithQueue(q=>q.HeartbeatAsync(id,owner,Lease,ct));}}
    private async Task<bool> CompleteCancellationIfRequested(IPlaceImportRepository repository,PlaceImportWorkItem work,CancellationToken ct)
    {
        if(!await repository.IsCancellationRequestedAsync(work.ImportId,ct))return false;
        await repository.CompleteCancellationAsync(work.ImportId,ct);
        await WithQueue(q=>q.CompleteAsync(work.ImportId,owner,ct));
        return true;
    }
    private async Task WithQueue(Func<IPlaceImportWorkQueue,Task> action){await using var s=scopes.CreateAsyncScope();await action(s.ServiceProvider.GetRequiredService<IPlaceImportWorkQueue>());}
    private sealed class PlaceSourceException(Failure failure):Exception(failure.Message){public Failure Failure{get;}=failure;}
}
