using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Places;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Places;

internal sealed class PlaceImportRepository(ZuppetoDbContext db) : IPlaceImportRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<PlaceImportScope> ResolveScopeAsync(Guid countryId, Guid territorialUnitId, CancellationToken ct = default)
    {
        var scope = await db.TerritorialUnits.AsNoTracking()
            .Where(x => x.Id == territorialUnitId && x.CountryId == countryId && x.IsActive)
            .Select(x => new
            {
                x.CountryId,
                CountryIso2 = x.Country.Iso2,
                x.Id,
                IsLocality = x.TerritorialUnitType.IsSelectableLocality || x.ManualSelectableLocality == true,
                Name = x.Names.Where(n => n.IsPrimary).Select(n => n.Name).FirstOrDefault(),
                Code = x.Codes.Where(c => c.Scheme == "es:ine:municipality" && c.IsPrimary)
                    .Select(c => new { c.Scheme, c.Value }).FirstOrDefault()
            }).SingleOrDefaultAsync(ct);
        if (scope is null || !scope.IsLocality || string.IsNullOrWhiteSpace(scope.CountryIso2)
            || string.IsNullOrWhiteSpace(scope.Name) || scope.Code is null)
            throw new InvalidOperationException("El municipi seleccionat no té identitat territorial oficial verificable.");
        return new(scope.CountryId, scope.CountryIso2, scope.Id, scope.Name, scope.Code.Scheme, scope.Code.Value);
    }

    public async Task<PlaceImportRunDto> CreateAsync(PreparePlaceImportRequest request, PlaceImportScope scope, Guid actorId, CancellationToken ct = default)
    {
        var source = await EnsureSourceAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var record = new PlaceImportRunRecord
        {
            Id = Guid.NewGuid(), SourceId = source.Id, CountryId = scope.CountryId, TerritorialUnitId = scope.TerritorialUnitId,
            CountryIso2 = scope.CountryIso2, TerritorialName = scope.TerritorialName,
            OfficialCodeScheme = scope.OfficialCodeScheme, OfficialCode = scope.OfficialCode,
            CategoriesJson = JsonSerializer.Serialize(request.Categories, Json), Status = PlaceImportStatus.Prepared.ToString(),
            AccessMode = request.AccessMode,
            Stage = "Prepared", SimulationOnly = true, CreatedByActorId = actorId, CreatedAtUtc = now, UpdatedAtUtc = now,
            NextAttemptAtUtc = now
        };
        db.PlaceImportRuns.Add(record);
        db.PlaceImportDecisions.Add(Decision(record.Id, actorId, "Prepared", "Importació OSM preparada en mode simulació.", now));
        await db.SaveChangesAsync(ct);
        return ToDto(record, source.SourceKey);
    }

    public async Task<IReadOnlyCollection<PlaceImportRunDto>> ListAsync(CancellationToken ct = default)
    {
        var records = await db.PlaceImportRuns.AsNoTracking().Include(x => x.Source)
            .OrderByDescending(x => x.CreatedAtUtc).ToArrayAsync(ct);
        return records.Select(x => ToDto(x, x.Source.SourceKey)).ToArray();
    }

    public async Task<PlaceImportDetailsDto?> GetAsync(Guid importId, CancellationToken ct = default)
    {
        var run = await db.PlaceImportRuns.AsNoTracking().Include(x => x.Source).SingleOrDefaultAsync(x => x.Id == importId, ct);
        if (run is null) return null;
        var itemRecords = await db.PlaceImportItems.AsNoTracking().Include(x => x.SourceRecord)
            .Where(x => x.ImportRunId == importId).OrderBy(x => x.Name).ToArrayAsync(ct);
        var items = itemRecords.Select(x => new PlaceImportItemDto(x.Id, x.SourceRecord.ExternalId, x.Name, x.Category,
            x.Latitude, x.Longitude, x.MatchBand, x.PlaceId, x.Status,
            JsonSerializer.Deserialize<Dictionary<string,string>>(x.NormalizedFieldsJson, Json) ?? new())).ToArray();
        var issues = await db.PlaceImportIssues.AsNoTracking().Where(x => x.ImportRunId == importId).OrderBy(x => x.CreatedAtUtc)
            .Select(x => new PlaceImportIssueDto(x.Id, x.Code, x.Severity, x.Message, x.ExternalId)).ToArrayAsync(ct);
        var decisions = await db.PlaceImportDecisions.AsNoTracking().Where(x => x.ImportRunId == importId).OrderByDescending(x => x.DecidedAtUtc)
            .Select(x => new PlaceImportDecisionDto(x.Id, x.Action, x.Reason, x.ActorId, x.DecidedAtUtc)).ToArrayAsync(ct);
        return new(ToDto(run, run.Source.SourceKey), items, issues, decisions);
    }

    public async Task StageAsync(Guid importId, PlaceDiscoveryBatch batch, IReadOnlyCollection<PlaceImportStagedItem> items,
        IReadOnlyCollection<PlaceImportIssueDto> issues, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var run = await db.PlaceImportRuns.Include(x => x.Source).SingleAsync(x => x.Id == importId, ct);
        PlaceImportGovernance.EnsureTransition(Enum.Parse<PlaceImportStatus>(run.Status), PlaceImportStatus.Validated);
        PlaceImportGovernance.EnsureTransition(PlaceImportStatus.Validated, PlaceImportStatus.PendingReview);
        var now = DateTimeOffset.UtcNow;
        foreach (var staged in items)
        {
            var sourceRecord = await db.PlaceSourceRecords.SingleOrDefaultAsync(x => x.SourceId == run.SourceId && x.ExternalId == staged.Source.ExternalId, ct);
            var payload = JsonSerializer.Serialize(staged.Candidate.Fields.ToDictionary(x => x.Key, x => x.Value.NormalizedValue), Json);
            if (sourceRecord is null)
            {
                sourceRecord = new PlaceSourceRecord
                {
                    Id = Guid.NewGuid(), SourceId = run.SourceId, ExternalId = staged.Source.ExternalId,
                    SourceVersion = staged.Source.SourceVersion, NormalizedPayloadJson = payload, ContentHash = staged.ContentHash,
                    EvidenceUri = staged.Source.EvidenceUri?.ToString() ?? string.Empty, ObservedAtUtc = staged.Source.ObservedAtUtc,
                    RetrievedAtUtc = now, FirstSeenAtUtc = now, LastSeenAtUtc = now, State = "Active"
                };
                db.PlaceSourceRecords.Add(sourceRecord);
            }
            else
            {
                sourceRecord.SourceVersion = staged.Source.SourceVersion; sourceRecord.NormalizedPayloadJson = payload;
                sourceRecord.ContentHash = staged.ContentHash; sourceRecord.ObservedAtUtc = staged.Source.ObservedAtUtc;
                sourceRecord.RetrievedAtUtc = now; sourceRecord.LastSeenAtUtc = now;
            }
            await db.SaveChangesAsync(ct);
            db.PlaceImportItems.Add(new PlaceImportItemRecord
            {
                Id = Guid.NewGuid(), ImportRunId = run.Id, SourceRecordId = sourceRecord.Id,
                Name = staged.Candidate.Name, Category = staged.Candidate.Category, Latitude = staged.Candidate.Latitude,
                Longitude = staged.Candidate.Longitude, NormalizedFieldsJson = payload, MatchBand = staged.Match.Band,
                MatchSignalsJson = JsonSerializer.Serialize(staged.Match.Signals, Json), MatchContradictionsJson = JsonSerializer.Serialize(staged.Match.Contradictions, Json),
                PlaceId = staged.Match.PlaceId, Status = staged.Match.RequiresHumanReview ? "PendingReview" : "Matched", IdempotencyHash = staged.ContentHash
            });
            foreach (var field in staged.Source.Fields)
            {
                db.PlaceFieldObservations.Add(new PlaceFieldObservationRecord
                {
                    Id = Guid.NewGuid(), SourceRecordId = sourceRecord.Id, ImportRunId = run.Id, PlaceId = staged.Match.PlaceId,
                    FieldName = field.Key, OriginalValue = field.Value.OriginalFormat, NormalizedValue = field.Value.NormalizedValue,
                    LicenseIdentifier = field.Value.LicenseIdentifier, Attribution = field.Value.Attribution,
                    Authority = "AUTO", State = "Current", ObservedAtUtc = field.Value.ObservedAtUtc, ValidUntilUtc = field.Value.ExpiresAtUtc
                });
            }
        }
        foreach (var issue in issues) db.PlaceImportIssues.Add(new PlaceImportIssueRecord
        {
            Id = issue.Id == Guid.Empty ? Guid.NewGuid() : issue.Id, ImportRunId = run.Id, ExternalId = issue.ExternalId,
            Code = issue.Code, Severity = issue.Severity, Message = issue.Message, CreatedAtUtc = now
        });
        run.BoundaryExternalId = batch.Scope.ExternalId; run.DiscoveredCount = batch.Documents.Count; run.ValidCount = items.Count;
        run.WarningCount = issues.Count; run.DuplicateCount = items.Count(x => x.Match.Band != "New");
        run.Status = PlaceImportStatus.PendingReview.ToString(); run.Stage = "Review"; run.UpdatedAtUtc = now; run.ProcessingCompletedAtUtc = now;
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }

    public Task ApproveAsync(Guid importId, Guid actorId, string reason, CancellationToken ct = default) =>
        DecideAsync(importId, actorId, reason, PlaceImportStatus.PendingReview, PlaceImportStatus.Approved, "Approved", ct);

    public Task SimulatePublicationAsync(Guid importId, Guid actorId, string reason, CancellationToken ct = default) =>
        DecideAsync(importId, actorId, reason, PlaceImportStatus.Approved, PlaceImportStatus.Published, "SimulationPublished", ct);

    public async Task CancelAsync(Guid importId, Guid actorId, CancellationToken ct = default)
    {
        var run = await db.PlaceImportRuns.SingleAsync(x => x.Id == importId, ct);
        var current = Enum.Parse<PlaceImportStatus>(run.Status);
        PlaceImportGovernance.EnsureTransition(current, PlaceImportStatus.Cancelled);
        var now = DateTimeOffset.UtcNow; run.CancellationRequested = true; run.UpdatedAtUtc = now;
        if (current != PlaceImportStatus.Running)
        {
            run.Status = PlaceImportStatus.Cancelled.ToString(); run.Stage = "Cancelled"; run.ProcessingCompletedAtUtc = now;
        }
        db.PlaceImportDecisions.Add(Decision(importId, actorId, current == PlaceImportStatus.Running ? "CancellationRequested" : "Cancelled", "Cancel·lació sol·licitada per l’administrador.", now));
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> IsCancellationRequestedAsync(Guid importId, CancellationToken ct = default) =>
        db.PlaceImportRuns.AsNoTracking().Where(x => x.Id == importId).Select(x => x.CancellationRequested).SingleAsync(ct);

    public async Task CompleteCancellationAsync(Guid importId, CancellationToken ct = default)
    {
        var run = await db.PlaceImportRuns.SingleAsync(x => x.Id == importId, ct);
        var current = Enum.Parse<PlaceImportStatus>(run.Status);
        if (current == PlaceImportStatus.Cancelled) return;
        PlaceImportGovernance.EnsureTransition(current, PlaceImportStatus.Cancelled);
        var now = DateTimeOffset.UtcNow;
        run.Status = PlaceImportStatus.Cancelled.ToString(); run.Stage = "Cancelled"; run.UpdatedAtUtc = now; run.ProcessingCompletedAtUtc = now;
        await db.SaveChangesAsync(ct);
    }

    private async Task DecideAsync(Guid importId, Guid actorId, string reason, PlaceImportStatus expected, PlaceImportStatus next, string action, CancellationToken ct)
    {
        var run = await db.PlaceImportRuns.SingleAsync(x => x.Id == importId, ct);
        var current = Enum.Parse<PlaceImportStatus>(run.Status);
        if (current != expected) throw new InvalidOperationException($"La importació ha d’estar en estat {expected}.");
        PlaceImportGovernance.EnsureTransition(current, next);
        if (next == PlaceImportStatus.Published && !run.SimulationOnly) throw new InvalidOperationException("La publicació real no està autoritzada.");
        var now = DateTimeOffset.UtcNow; run.Status = next.ToString(); run.Stage = next == PlaceImportStatus.Published ? "SimulationCompleted" : "Approved"; run.UpdatedAtUtc = now;
        db.PlaceImportDecisions.Add(Decision(importId, actorId, action, reason, now)); await db.SaveChangesAsync(ct);
    }

    private async Task<PlaceDatasetSourceRecord> EnsureSourceAsync(CancellationToken ct)
    {
        var source = await db.PlaceDatasetSources.SingleOrDefaultAsync(x => x.SourceKey == OpenStreetMapSource.SourceKey, ct);
        if (source is not null) return source;
        source = new PlaceDatasetSourceRecord
        {
            Id = Guid.NewGuid(), SourceKey = OpenStreetMapSource.SourceKey, Organisation = "OpenStreetMap Foundation / contributors",
            Dataset = "OpenStreetMap", Url = "https://www.openstreetmap.org",
            AttributionUrl = "https://www.openstreetmap.org/copyright", LicenseIdentifier = "ODbL-1.0",
            LicenseUrl = "https://opendatacommons.org/licenses/odbl/1-0/", Attribution = "© OpenStreetMap contributors",
            ReuseConditions = "Traçabilitat i atribució obligatòries; qualificació Derivative/Collective pendent de revisió jurídica.",
            PermanentStorageAllowed = true, OdblDerived = true, ApprovalStatus = "PilotApproved",
            ReviewedAtUtc = new DateTimeOffset(2026,10,13,0,0,0,TimeSpan.Zero), CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.PlaceDatasetSources.Add(source); await db.SaveChangesAsync(ct); return source;
    }

    private static PlaceImportDecisionRecord Decision(Guid runId, Guid actorId, string action, string reason, DateTimeOffset now) =>
        new() { Id = Guid.NewGuid(), ImportRunId = runId, ActorId = actorId, Action = action, Reason = reason, DecidedAtUtc = now };

    private static PlaceImportRunDto ToDto(PlaceImportRunRecord x, string sourceKey) => new(x.Id, sourceKey, x.AccessMode,
        new(x.CountryId,x.CountryIso2,x.TerritorialUnitId,x.TerritorialName,x.OfficialCodeScheme,x.OfficialCode),
        JsonSerializer.Deserialize<string[]>(x.CategoriesJson, Json) ?? [], x.Status, x.Stage, x.SimulationOnly,
        x.DiscoveredCount,x.ValidCount,x.WarningCount,x.DuplicateCount,x.BoundaryExternalId,x.CreatedAtUtc,x.UpdatedAtUtc,x.ErrorCode,x.ErrorMessage);
}
