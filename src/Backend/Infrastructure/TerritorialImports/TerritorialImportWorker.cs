using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zuppeto.Application.TerritorialImports;

namespace Zuppeto.Infrastructure.TerritorialImports;

internal sealed class TerritorialImportWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<TerritorialImportWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);
    private const int MaximumAttempts = 3;
    private readonly string owner = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var work = await ClaimAsync(stoppingToken);
                if (work is null)
                {
                    await Task.Delay(PollInterval, stoppingToken);
                    continue;
                }

                await ProcessAsync(work, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error no associat a cap treball concret al worker territorial.");
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }

    private async Task<TerritorialImportWorkItem?> ClaimAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var queue = scope.ServiceProvider.GetRequiredService<ITerritorialImportWorkQueue>();
        return await queue.ClaimNextAsync(owner, LeaseDuration, ct);
    }

    private async Task ProcessAsync(TerritorialImportWorkItem work, CancellationToken stoppingToken)
    {
        using var heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var heartbeat = MaintainLeaseAsync(work.ImportId, heartbeatCancellation.Token);
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<TerritorialImportService>();
            if (work.IsPublication) await service.ProcessPublicationAsync(work, stoppingToken);
            else await service.ProcessPreparationAsync(work, stoppingToken);

            var queue = scope.ServiceProvider.GetRequiredService<ITerritorialImportWorkQueue>();
            await queue.CompleteAsync(work.ImportId, owner, stoppingToken);
        }
        catch (TerritorialImportCancellationException)
        {
            await WithQueueAsync(queue => queue.CancelClaimedAsync(work.ImportId, owner, stoppingToken));
            logger.LogInformation("Importació territorial {ImportId} cancel·lada cooperativament.", work.ImportId);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            var failure = TerritorialWorkerFailureClassifier.Classify(exception);
            await WithQueueAsync(queue => queue.FailAsync(work.ImportId, owner, failure.Code,
                failure.SafeMessage, failure.Recoverable, MaximumAttempts, stoppingToken));
            logger.LogError(exception, "Ha fallat la importació territorial {ImportId} ({Code}).", work.ImportId, failure.Code);
        }
        finally
        {
            heartbeatCancellation.Cancel();
            try { await heartbeat; }
            catch (OperationCanceledException) { }
        }
    }

    private async Task MaintainLeaseAsync(Guid importId, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(HeartbeatInterval, ct);
            await WithQueueAsync(queue => queue.HeartbeatAsync(importId, owner, LeaseDuration, ct));
        }
    }

    private async Task WithQueueAsync(Func<ITerritorialImportWorkQueue, Task> action)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<ITerritorialImportWorkQueue>());
    }

}

internal static class TerritorialWorkerFailureClassifier
{
    internal static TerritorialWorkerFailure Classify(Exception exception) => exception switch
    {
        DbUpdateConcurrencyException => new("CATALOG_VERSION_CONFLICT", "El catàleg ha canviat; cal revisar de nou la importació.", false),
        InvalidDataException => new("XLSX_INVALID", "L'artefacte XLSX no és vàlid o no es pot llegir.", false),
        InvalidOperationException => new("IMPORT_CONTRACT_INVALID", "La configuració o les dades de la importació no compleixen el contracte territorial.", false),
        TimeoutException or DbException or IOException => new("WORKER_TRANSIENT", "Error temporal durant el processament; es tornarà a intentar.", true),
        _ => new("WORKER_FAILED", "La importació ha fallat durant el processament.", false)
    };
}

internal sealed record TerritorialWorkerFailure(string Code, string SafeMessage, bool Recoverable);
