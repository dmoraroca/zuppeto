using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ValueObjects;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;
using Zuppeto.Infrastructure.Persistence.Mappings;

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
                Name = x.Names.Where(n => n.IsPrimary).Select(n => n.Name).FirstOrDefault(),
                Codes = x.Codes.Where(c => c.IsPrimary || c.Scheme == "es:ine:municipality"
                        || c.Scheme == "de:destatis:ags" || c.Scheme == "de:destatis:land")
                    .Select(c => new { c.Scheme, c.Value, c.IsPrimary }).ToArray()
            }).SingleOrDefaultAsync(ct);
        if (scope is null || string.IsNullOrWhiteSpace(scope.CountryIso2)
            || string.IsNullOrWhiteSpace(scope.Name))
            throw new InvalidOperationException("La unitat territorial seleccionada no té identitat oficial verificable.");
        var code = scope.Codes
            .OrderBy(item => PreferredCodeOrder(scope.CountryIso2, item.Scheme))
            .ThenByDescending(item => item.IsPrimary)
            .FirstOrDefault();
        if (code is null)
            throw new InvalidOperationException("El municipi seleccionat no té cap codi territorial oficial admès.");
        return new(scope.CountryId, scope.CountryIso2, scope.Id, scope.Name, code.Scheme, code.Value);
    }

    private static int PreferredCodeOrder(string countryIso2, string scheme) => (countryIso2.ToUpperInvariant(), scheme) switch
    {
        ("ES", "es:ine:municipality") => 0,
        ("DE", "de:destatis:ags") => 0,
        ("DE", "de:destatis:land") => 0,
        _ => 1
    };

    public async Task<PlaceImportRunDto> CreateAsync(PreparePlaceImportRequest request, PlaceImportScope scope, Guid actorId, CancellationToken ct = default)
    {
        var source = await EnsureSourceAsync(request.SourceKey ?? OpenStreetMapSource.SourceKey, ct);
        var now = DateTimeOffset.UtcNow;
        var record = new PlaceImportRunRecord
        {
            Id = Guid.NewGuid(), SourceId = source.Id, CountryId = scope.CountryId, TerritorialUnitId = scope.TerritorialUnitId,
            CountryIso2 = scope.CountryIso2, TerritorialName = scope.TerritorialName,
            OfficialCodeScheme = scope.OfficialCodeScheme, OfficialCode = scope.OfficialCode,
            CategoriesJson = JsonSerializer.Serialize(request.Categories, Json), Status = PlaceImportStatus.Prepared.ToString(),
            AccessMode = request.AccessMode,
            Stage = "Prepared", SimulationOnly = request.SimulationOnly, CreatedByActorId = actorId, CreatedAtUtc = now, UpdatedAtUtc = now,
            NextAttemptAtUtc = now
        };
        db.PlaceImportRuns.Add(record);
        db.PlaceImportDecisions.Add(Decision(record.Id, actorId, "Prepared", request.SimulationOnly
            ? $"Importació {source.SourceKey} preparada en mode simulació."
            : $"Importació {source.SourceKey} real preparada per al pilot controlat d’Arenys de Mar.", now));
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
            var contentChanged = sourceRecord is null || !string.Equals(sourceRecord.ContentHash, staged.ContentHash, StringComparison.Ordinal);
            var sourcePayload = staged.Source.Fields.ToDictionary(x => x.Key, x => x.Value.NormalizedValue);
            var payload = JsonSerializer.Serialize(sourcePayload, Json);
            var normalizedPayload = JsonSerializer.Serialize(staged.Candidate.Fields.ToDictionary(x => x.Key, x => x.Value.NormalizedValue), Json);
            if (sourceRecord is null)
            {
                sourceRecord = new PlaceSourceRecord
                {
                    Id = Guid.NewGuid(), SourceId = run.SourceId, ExternalId = staged.Source.ExternalId,
                    SourceVersion = staged.Source.SourceVersion, NormalizedPayloadJson = payload, ContentHash = staged.ContentHash,
                    EvidenceUri = staged.Source.EvidenceUri?.ToString() ?? string.Empty, ObservedAtUtc = staged.Source.ObservedAtUtc,
                    RetrievedAtUtc = now, FirstSeenAtUtc = now, LastSeenAtUtc = now, State = "Active",
                    MatchStatus = staged.Match.RequiresHumanReview ? "PendingReview" : staged.Match.Band,
                    VerificationStatus = "Pending"
                };
                db.PlaceSourceRecords.Add(sourceRecord);
            }
            else
            {
                sourceRecord.SourceVersion = staged.Source.SourceVersion; sourceRecord.NormalizedPayloadJson = payload;
                sourceRecord.ContentHash = staged.ContentHash; sourceRecord.ObservedAtUtc = staged.Source.ObservedAtUtc;
                sourceRecord.RetrievedAtUtc = now; sourceRecord.LastSeenAtUtc = now;
                sourceRecord.MatchStatus = staged.Match.RequiresHumanReview ? "PendingReview" : staged.Match.Band;
            }
            await db.SaveChangesAsync(ct);
            db.PlaceImportItems.Add(new PlaceImportItemRecord
            {
                Id = Guid.NewGuid(), ImportRunId = run.Id, SourceRecordId = sourceRecord.Id,
                Name = staged.Candidate.Name, Category = staged.Candidate.Category, Latitude = staged.Candidate.Latitude,
                Longitude = staged.Candidate.Longitude, NormalizedFieldsJson = normalizedPayload, MatchBand = staged.Match.Band,
                MatchSignalsJson = JsonSerializer.Serialize(staged.Match.Signals, Json), MatchContradictionsJson = JsonSerializer.Serialize(staged.Match.Contradictions, Json),
                PlaceId = staged.Match.PlaceId, Status = staged.Match.RequiresHumanReview ? "PendingReview" : "Matched", IdempotencyHash = staged.ContentHash
            });
            if (contentChanged && sourceRecord.CanonicalPlaceId is not null)
            {
                var superseded = await db.PlaceFieldObservations.Where(x => x.SourceRecordId == sourceRecord.Id
                    && x.State == "Current" && x.Authority == "AUTO").ToArrayAsync(ct);
                foreach (var observation in superseded) observation.State = "Superseded";
            }
            if (contentChanged)
            {
                foreach (var field in staged.Source.Fields)
                {
                    db.PlaceFieldObservations.Add(new PlaceFieldObservationRecord
                    {
                        Id = Guid.NewGuid(), SourceRecordId = sourceRecord.Id, ImportRunId = run.Id, PlaceId = staged.Match.PlaceId,
                        FieldName = field.Key, OriginalValue = field.Value.OriginalFormat, NormalizedValue = field.Value.NormalizedValue,
                        LicenseIdentifier = field.Value.LicenseIdentifier, Attribution = field.Value.Attribution,
                        Authority = "AUTO", State = "Current", ObservedAtUtc = field.Value.ObservedAtUtc,
                        ValidUntilUtc = field.Value.ExpiresAtUtc, VerificationStatus = "Pending"
                    });
                }
            }
            var previousDecisions = await db.PlaceMatchDecisions.Where(x => x.SourceRecordId == sourceRecord.Id && x.IsCurrent).ToArrayAsync(ct);
            foreach (var previous in previousDecisions) { previous.IsCurrent = false; previous.ReversedAtUtc = now; }
            db.PlaceMatchDecisions.Add(new PlaceMatchDecisionRecord
            {
                Id = Guid.NewGuid(), SourceRecordId = sourceRecord.Id, CandidatePlaceId = staged.Match.PlaceId,
                Outcome = ToOutcome(staged.Match), Score = MatchScore(staged.Match),
                SignalsJson = JsonSerializer.Serialize(staged.Match.Signals, Json), ContradictionsJson = JsonSerializer.Serialize(staged.Match.Contradictions, Json),
                IsCurrent = true, Reason = "Decisió automàtica explicable de la pipeline.", DecidedAtUtc = now
            });
            foreach (var enrichment in staged.Enrichments ?? [])
            {
                var enrichmentSource = await EnsureSourceAsync(enrichment.SourceKey, ct);
                var enrichmentPayload = JsonSerializer.Serialize(enrichment.Fields.ToDictionary(x => x.Key, x => x.Value.NormalizedValue), Json);
                var enrichmentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(enrichmentPayload))).ToLowerInvariant();
                var enrichmentRecord = await db.PlaceSourceRecords.SingleOrDefaultAsync(x => x.SourceId == enrichmentSource.Id && x.ExternalId == enrichment.ExternalId, ct);
                if (enrichmentRecord is null)
                {
                    enrichmentRecord = new PlaceSourceRecord { Id = Guid.NewGuid(), SourceId = enrichmentSource.Id, ExternalId = enrichment.ExternalId,
                        SourceVersion = enrichment.SourceVersion, NormalizedPayloadJson = enrichmentPayload, ContentHash = enrichmentHash,
                        EvidenceUri = enrichment.EvidenceUri?.ToString() ?? string.Empty, ObservedAtUtc = enrichment.ObservedAtUtc,
                        RetrievedAtUtc = now, FirstSeenAtUtc = now, LastSeenAtUtc = now, State = "Active", CanonicalPlaceId = staged.Match.PlaceId };
                    enrichmentRecord.MatchStatus = staged.Match.PlaceId is null ? "PendingReview" : "Matched";
                    enrichmentRecord.VerificationStatus = "Pending";
                    db.PlaceSourceRecords.Add(enrichmentRecord);
                }
                else
                {
                    enrichmentRecord.SourceVersion = enrichment.SourceVersion; enrichmentRecord.NormalizedPayloadJson = enrichmentPayload;
                    enrichmentRecord.ContentHash = enrichmentHash; enrichmentRecord.ObservedAtUtc = enrichment.ObservedAtUtc;
                    enrichmentRecord.RetrievedAtUtc = now; enrichmentRecord.LastSeenAtUtc = now;
                }
                await db.SaveChangesAsync(ct);
                db.PlaceImportEnrichments.Add(new PlaceImportEnrichmentRecord { Id = Guid.NewGuid(), ImportRunId = run.Id,
                    ParentSourceRecordId = sourceRecord.Id, EnrichmentSourceRecordId = enrichmentRecord.Id,
                    PlaceId = staged.Match.PlaceId, CreatedAtUtc = now });
                foreach (var field in enrichment.Fields)
                    db.PlaceFieldObservations.Add(new PlaceFieldObservationRecord { Id = Guid.NewGuid(), SourceRecordId = enrichmentRecord.Id,
                        ImportRunId = run.Id, PlaceId = staged.Match.PlaceId, FieldName = field.Key, OriginalValue = field.Value.OriginalFormat,
                        NormalizedValue = field.Value.NormalizedValue, LicenseIdentifier = field.Value.LicenseIdentifier,
                        Attribution = field.Value.Attribution, Authority = "AUTO", State = "Current",
                        ObservedAtUtc = field.Value.ObservedAtUtc, ValidUntilUtc = field.Value.ExpiresAtUtc,
                        VerificationStatus = "Pending" });
            }
        }
        foreach (var issue in issues) db.PlaceImportIssues.Add(new PlaceImportIssueRecord
        {
            Id = issue.Id == Guid.Empty ? Guid.NewGuid() : issue.Id, ImportRunId = run.Id, ExternalId = issue.ExternalId,
            Code = issue.Code, Severity = issue.Severity, Message = issue.Message, CreatedAtUtc = now
        });
        run.BoundaryExternalId = batch.Scope.ExternalId; run.DiscoveredCount = batch.Documents.Count; run.ValidCount = items.Count;
        run.WarningCount = issues.Count; run.DuplicateCount = items.Count(x => x.Match.Band != PlaceMatchOutcome.NotMatch.ToString());
        run.Status = PlaceImportStatus.PendingReview.ToString(); run.Stage = "Review"; run.UpdatedAtUtc = now; run.ProcessingCompletedAtUtc = now;
        await db.SaveChangesAsync(ct);
        foreach (var placeId in items.Select(x => x.Match.PlaceId).OfType<Guid>().Distinct()) await DetectConflicts(placeId, now, ct);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }

    public async Task SaveCheckpointAsync(Guid importId, PlaceSourceCheckpointSnapshot checkpoint, CancellationToken ct = default)
    {
        var sourceId = await db.PlaceImportRuns.Where(x => x.Id == importId).Select(x => x.SourceId).SingleAsync(ct);
        var existing = await db.PlaceSourceCheckpoints.SingleOrDefaultAsync(x => x.SourceId == sourceId
            && x.RegionKey == checkpoint.RegionKey && x.Generation == checkpoint.Generation, ct);
        if (existing is null)
        {
            db.PlaceSourceCheckpoints.Add(new PlaceSourceCheckpointRecord { Id = Guid.NewGuid(), SourceId = sourceId,
                RegionKey = checkpoint.RegionKey, Generation = checkpoint.Generation, ExtractSha256 = checkpoint.ExtractSha256,
                ReplicationSequence = checkpoint.ReplicationSequence, Status = checkpoint.Status, MetricsJson = checkpoint.MetricsJson,
                CreatedAtUtc = checkpoint.CreatedAtUtc, CompletedAtUtc = checkpoint.CompletedAtUtc });
        }
        else
        {
            existing.ExtractSha256 = checkpoint.ExtractSha256; existing.ReplicationSequence = checkpoint.ReplicationSequence;
            existing.Status = checkpoint.Status; existing.MetricsJson = checkpoint.MetricsJson; existing.CompletedAtUtc = checkpoint.CompletedAtUtc;
        }
        await db.SaveChangesAsync(ct);
    }

    public Task ApproveAsync(Guid importId, Guid actorId, string reason, CancellationToken ct = default) =>
        DecideAsync(importId, actorId, reason, PlaceImportStatus.PendingReview, PlaceImportStatus.Approved, "Approved", ct);

    public Task SimulatePublicationAsync(Guid importId, Guid actorId, string reason, CancellationToken ct = default) =>
        DecideAsync(importId, actorId, reason, PlaceImportStatus.Approved, PlaceImportStatus.Published, "SimulationPublished", ct);

    public async Task PublishAsync(Guid importId, Guid actorId, string reason, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var run = await db.PlaceImportRuns.Include(x => x.Source).SingleAsync(x => x.Id == importId, ct);
        var current = Enum.Parse<PlaceImportStatus>(run.Status);
        if (current != PlaceImportStatus.Approved) throw new InvalidOperationException("La importació ha d’estar en estat Approved.");
        PlaceImportGovernance.EnsureTransition(current, PlaceImportStatus.Published);
        if (run.SimulationOnly) throw new InvalidOperationException("Una simulació no es pot publicar al catàleg real.");
        var categories = JsonSerializer.Deserialize<string[]>(run.CategoriesJson, Json) ?? [];
        var osmPilot = run.Source.SourceKey == OpenStreetMapSource.SourceKey
            && run.AccessMode == OpenStreetMapSource.GeofabrikExtractAccessMode;
        var overturePilot = run.Source.SourceKey == OvertureMapsSource.SourceKey
            && run.AccessMode == OvertureMapsSource.LocalExtractAccessMode
            && run.BoundaryExternalId?.StartsWith("overture-boundary:", StringComparison.Ordinal) == true
            && IsExactOvertureBasicCategorySet(categories);
        if ((!osmPilot && !overturePilot)
            || run.TerritorialUnitId != Guid.Parse("8680aa5a-292f-4201-8d52-d6381f438af4")
            || run.CountryIso2 != "ES" || run.OfficialCode != "08006")
            throw new InvalidOperationException("La publicació real no correspon al pilot autoritzat d’Arenys de Mar.");
        if (await db.PlaceImportIssues.AnyAsync(x => x.ImportRunId == importId && x.Severity == "Error", ct))
            throw new InvalidOperationException("La importació conté conflictes bloquejants.");

        var items = await db.PlaceImportItems.Include(x => x.SourceRecord)
            .Where(x => x.ImportRunId == importId).OrderBy(x => x.SourceRecord.ExternalId).ToArrayAsync(ct);
        var now = DateTimeOffset.UtcNow;
        foreach (var item in items)
        {
            var matchedPlaceId = item.SourceRecord.CanonicalPlaceId
                ?? (item.Status != "PendingReview" ? item.PlaceId : null);
            if (matchedPlaceId is Guid existingPlaceId)
            {
                item.SourceRecord.CanonicalPlaceId = existingPlaceId;
                item.SourceRecord.MatchStatus = "Matched";
                item.SourceRecord.VerificationStatus = "Verified";
                item.SourceRecord.VerifiedAtUtc ??= now;
                item.PlaceId = existingPlaceId;
                item.Status = "Existing";
                await LinkObservations(importId, item.SourceRecordId, existingPlaceId, ct);
                await LinkEnrichments(importId, item.SourceRecordId, existingPlaceId, ct);
                continue;
            }
            if (item.Status == "PendingReview")
            {
                item.Status = "RejectedConflict";
                item.SourceRecord.MatchStatus = "PendingReview";
                continue;
            }
            if (item.Latitude is null || item.Longitude is null)
                throw new InvalidOperationException($"El POI {item.SourceRecord.ExternalId} no té coordenades publicables.");

            var fields = JsonSerializer.Deserialize<Dictionary<string,string>>(item.NormalizedFieldsJson, Json) ?? new();
            var place = BuildPlace(item, fields, run);
            var record = PlacePersistenceMapper.ToRecord(place);
            PlacePersistenceMapper.SyncCollections(place, record);
            record.CreatedAtUtc = now;
            record.UpdatedAtUtc = now;
            db.Places.Add(record);
            item.SourceRecord.CanonicalPlaceId = place.Id;
            item.SourceRecord.MatchStatus = "Matched";
            item.SourceRecord.VerificationStatus = "Verified";
            item.SourceRecord.VerifiedAtUtc ??= now;
            item.PlaceId = place.Id;
            item.Status = "Published";
            await LinkObservations(importId, item.SourceRecordId, place.Id, ct);
            await LinkEnrichments(importId, item.SourceRecordId, place.Id, ct);
        }

        run.Status = PlaceImportStatus.Published.ToString();
        run.Stage = "PublicationCompleted";
        run.UpdatedAtUtc = now;
        db.PlaceImportDecisions.Add(Decision(importId, actorId, "Published", reason, now));
        await db.SaveChangesAsync(ct);
        foreach (var placeId in items.Select(x => x.PlaceId).OfType<Guid>().Distinct()) await DetectConflicts(placeId, now, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

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
        if (next == PlaceImportStatus.Published && !run.SimulationOnly) throw new InvalidOperationException("La importació real s’ha de publicar amb el cas d’ús controlat.");
        var now = DateTimeOffset.UtcNow; run.Status = next.ToString(); run.Stage = next == PlaceImportStatus.Published ? "SimulationCompleted" : "Approved"; run.UpdatedAtUtc = now;
        db.PlaceImportDecisions.Add(Decision(importId, actorId, action, reason, now)); await db.SaveChangesAsync(ct);
    }

    private async Task<PlaceDatasetSourceRecord> EnsureSourceAsync(string sourceKey, CancellationToken ct)
    {
        var source = await db.PlaceDatasetSources.SingleOrDefaultAsync(x => x.SourceKey == sourceKey, ct);
        if (source is not null) return source;
        if (sourceKey == WikidataSource.SourceKey)
        {
            source = new PlaceDatasetSourceRecord
            {
                Id = Guid.NewGuid(), SourceKey = WikidataSource.SourceKey, Organisation = "Wikimedia Foundation / comunitat Wikidata",
                Dataset = "Wikidata structured data", DatasetVersion = "live", Coverage = "Global; enriquiment per QID explícit",
                Url = "https://www.wikidata.org", AttributionUrl = "https://www.wikidata.org/wiki/Wikidata:Licensing",
                LicenseIdentifier = "CC0-1.0", LicenseUrl = "https://creativecommons.org/publicdomain/zero/1.0/",
                Attribution = "Dades de Wikidata", ReuseConditions = "Només dades estructurades; no s’importen textos o imatges enllaçats.",
                PermanentStorageAllowed = true, CommercialUseAllowed = true, OdblDerived = false, ApprovalStatus = "PilotApproved",
                ReviewedAtUtc = new DateTimeOffset(2026,10,14,0,0,0,TimeSpan.Zero), CreatedAtUtc = DateTimeOffset.UtcNow
            };
            await SaveGovernedSourceAsync(source, "wikimedia-foundation", "CC0-1.0", ct); return source;
        }
        if (sourceKey == DibaMunicipalSource.SourceKey)
        {
            source = new PlaceDatasetSourceRecord
            {
                Id = Guid.NewGuid(), SourceKey = DibaMunicipalSource.SourceKey,
                Organisation = "Diputació de Barcelona", Dataset = "Cens municipal d’activitats i establiments",
                DatasetVersion = "live", Coverage = "Municipis participants de la demarcació de Barcelona",
                Url = "https://dadesobertes.diba.cat/datasets/cens-dactivitats-i-establiments",
                AttributionUrl = "https://creativecommons.org/licenses/by/4.0/",
                LicenseIdentifier = "CC-BY-4.0", LicenseUrl = "https://creativecommons.org/licenses/by/4.0/",
                Attribution = "Font: Diputació de Barcelona — Cens municipal d’activitats i establiments",
                ReuseConditions = "Atribució i data d’actualització obligatòries; NIF i raó social exclosos per minimització.",
                PermanentStorageAllowed = true, CommercialUseAllowed = true, OdblDerived = false,
                ApprovalStatus = "PilotApproved",
                ReviewedAtUtc = new DateTimeOffset(2026,10,17,0,0,0,TimeSpan.Zero), CreatedAtUtc = DateTimeOffset.UtcNow
            };
            await SaveGovernedSourceAsync(source, "diputacio-barcelona", "CC-BY-4.0", ct); return source;
        }
        if (sourceKey == OvertureMapsSource.SourceKey)
        {
            source = new PlaceDatasetSourceRecord
            {
                Id = Guid.NewGuid(), SourceKey = OvertureMapsSource.SourceKey,
                Organisation = "Overture Maps Foundation i proveïdors",
                Dataset = "Overture Maps Places", DatasetVersion = OvertureMapsSource.ReleaseVersion,
                Coverage = "Global; selecció territorial verificada per geometria",
                Url = "https://overturemaps.org/download/", AttributionUrl = "https://docs.overturemaps.org/attribution/",
                LicenseIdentifier = "CDLA-Permissive-2.0+Apache-2.0+CC0-1.0",
                LicenseUrl = "https://docs.overturemaps.org/attribution/",
                Attribution = "© Overture Maps Foundation i proveïdors de les dades",
                ReuseConditions = "Conservar GERS ID, release, datasets d’origen, llicències i atribució de cada observació.",
                PermanentStorageAllowed = true, CommercialUseAllowed = true, OdblDerived = false,
                ApprovalStatus = "PilotApproved",
                ReviewedAtUtc = new DateTimeOffset(2026,10,18,0,0,0,TimeSpan.Zero), CreatedAtUtc = DateTimeOffset.UtcNow
            };
            await SaveGovernedSourceAsync(source, "overture-maps-foundation", "CDLA-Permissive-2.0", ct); return source;
        }
        if (sourceKey == CataloniaTourismSource.SourceKey)
        {
            source = new PlaceDatasetSourceRecord
            {
                Id = Guid.NewGuid(), SourceKey = CataloniaTourismSource.SourceKey,
                Organisation = "Generalitat de Catalunya · Departament d’Empresa i Treball",
                Dataset = "Registre de Turisme de Catalunya", DatasetVersion = "live",
                Coverage = "Catalunya; allotjaments turístics inscrits",
                Url = "https://analisi.transparenciacatalunya.cat/d/t2h3-cgys",
                AttributionUrl = "https://web.gencat.cat/ca/generalitat/dades-indicadors/dades-obertes/llicencies",
                LicenseIdentifier = "CATALONIA-OPEN-DATA",
                LicenseUrl = "https://web.gencat.cat/ca/generalitat/dades-indicadors/dades-obertes/llicencies",
                Attribution = "Generalitat de Catalunya · Departament d’Empresa i Treball",
                ReuseConditions = "Atribució i data de consulta obligatòries; s’exclouen CIF i identitat del titular.",
                PermanentStorageAllowed = true, CommercialUseAllowed = true, OdblDerived = false,
                ApprovalStatus = "PilotApproved",
                ReviewedAtUtc = new DateTimeOffset(2026,10,18,0,0,0,TimeSpan.Zero), CreatedAtUtc = DateTimeOffset.UtcNow
            };
            await SaveGovernedSourceAsync(source, "generalitat-catalunya", "CATALONIA-OPEN-DATA", ct); return source;
        }
        if (sourceKey == AragonCommercialSource.SourceKey)
        {
            source = new PlaceDatasetSourceRecord
            {
                Id = Guid.NewGuid(), SourceKey = AragonCommercialSource.SourceKey,
                Organisation = "Gobierno de Aragón", Dataset = "Registro de Actividades Comerciales",
                DatasetVersion = "live", Coverage = "Aragó; establiments del registre comercial",
                Url = "https://opendata.aragon.es/datos/catalogo/dataset/registro-de-actividades-comerciales",
                AttributionUrl = "https://creativecommons.org/licenses/by/4.0/",
                LicenseIdentifier = "CC-BY-4.0", LicenseUrl = "https://creativecommons.org/licenses/by/4.0/",
                Attribution = "Gobierno de Aragón · Registro de Actividades Comerciales",
                ReuseConditions = "Atribució i data de consulta obligatòries; candidats sense coordenades pendents de corroboració.",
                PermanentStorageAllowed = true, CommercialUseAllowed = true, OdblDerived = false,
                ApprovalStatus = "PilotApproved",
                ReviewedAtUtc = new DateTimeOffset(2026,10,18,0,0,0,TimeSpan.Zero), CreatedAtUtc = DateTimeOffset.UtcNow
            };
            await SaveGovernedSourceAsync(source, "gobierno-aragon", "CC-BY-4.0", ct); return source;
        }
        if (sourceKey != OpenStreetMapSource.SourceKey)
            throw new InvalidOperationException($"La font d’importació '{sourceKey}' no està registrada.");
        source = new PlaceDatasetSourceRecord
        {
            Id = Guid.NewGuid(), SourceKey = OpenStreetMapSource.SourceKey, Organisation = "OpenStreetMap Foundation / contributors",
            Dataset = "OpenStreetMap", DatasetVersion = "live", Coverage = "Pilot Catalunya / Arenys de Mar", Url = "https://www.openstreetmap.org",
            AttributionUrl = "https://www.openstreetmap.org/copyright", LicenseIdentifier = "ODbL-1.0",
            LicenseUrl = "https://opendatacommons.org/licenses/odbl/1-0/", Attribution = "© OpenStreetMap contributors",
            ReuseConditions = "Traçabilitat i atribució obligatòries; qualificació Derivative/Collective pendent de revisió jurídica.",
            PermanentStorageAllowed = true, CommercialUseAllowed = true, OdblDerived = true, ApprovalStatus = "PilotApproved",
            ReviewedAtUtc = new DateTimeOffset(2026,10,13,0,0,0,TimeSpan.Zero), CreatedAtUtc = DateTimeOffset.UtcNow
        };
        await SaveGovernedSourceAsync(source, "openstreetmap-foundation", "ODbL-1.0", ct); return source;
    }

    private async Task SaveGovernedSourceAsync(
        PlaceDatasetSourceRecord source,
        string organisationKey,
        string licenseCode,
        CancellationToken ct)
    {
        var organisation = await db.PlaceDataOrganisations
            .SingleOrDefaultAsync(item => item.OrganisationKey == organisationKey, ct);
        var license = await db.PlaceDataLicenses
            .SingleOrDefaultAsync(item => item.Code == licenseCode, ct);
        source.OrganisationId = organisation?.Id;
        source.LicenseId = license?.Id;
        source.ConsultedAtUtc = DateTimeOffset.UtcNow;
        source.ReuseStatus = organisation is not null && license?.CommercialUseStatus == "Allowed"
            ? "Allowed"
            : "Unknown";
        db.PlaceDatasetSources.Add(source);
        await db.SaveChangesAsync(ct);
    }

    private async Task LinkEnrichments(Guid importId, Guid parentSourceRecordId, Guid placeId, CancellationToken ct)
    {
        var links = await db.PlaceImportEnrichments.Where(x => x.ImportRunId == importId && x.ParentSourceRecordId == parentSourceRecordId)
            .Include(x => x.EnrichmentSourceRecord).ToArrayAsync(ct);
        foreach (var link in links)
        {
            link.PlaceId = placeId; link.EnrichmentSourceRecord.CanonicalPlaceId = placeId;
            link.EnrichmentSourceRecord.MatchStatus = "Matched";
            link.EnrichmentSourceRecord.VerificationStatus = "Verified";
            link.EnrichmentSourceRecord.VerifiedAtUtc ??= DateTimeOffset.UtcNow;
            await LinkObservations(importId, link.EnrichmentSourceRecordId, placeId, ct);
        }
        var decisions = await db.PlaceMatchDecisions.Where(x => x.SourceRecordId == parentSourceRecordId && x.IsCurrent).ToArrayAsync(ct);
        foreach (var decision in decisions) decision.CandidatePlaceId = placeId;
    }

    private async Task DetectConflicts(Guid placeId, DateTimeOffset now, CancellationToken ct)
    {
        var observations = await db.PlaceFieldObservations.Include(x => x.SourceRecord)
            .Where(x => x.PlaceId == placeId && x.State == "Current").OrderByDescending(x => x.ObservedAtUtc).ToArrayAsync(ct);
        foreach (var group in observations.GroupBy(x => x.FieldName, StringComparer.OrdinalIgnoreCase))
        {
            var distinct = group.GroupBy(x => x.NormalizedValue, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).Take(2).ToArray();
            if (distinct.Length < 2 || distinct[0].SourceRecordId == distinct[1].SourceRecordId) continue;
            var left = distinct[0]; var right = distinct[1];
            if (await db.PlaceSourceConflicts.AnyAsync(x => x.PlaceId == placeId && x.FieldName == group.Key && x.Status == "Open"
                && ((x.LeftObservationId == left.Id && x.RightObservationId == right.Id) || (x.LeftObservationId == right.Id && x.RightObservationId == left.Id)), ct)) continue;
            db.PlaceSourceConflicts.Add(new PlaceSourceConflictRecord { Id = Guid.NewGuid(), PlaceId = placeId, FieldName = group.Key,
                LeftObservationId = left.Id, RightObservationId = right.Id, Status = "Open", ResolutionReason = string.Empty, DetectedAtUtc = now });
        }
    }

    private static string ToOutcome(PlaceMatchDecision match) => match.Band switch
    {
        "ExactExternalIdentity" or "ConfirmedMatch" => PlaceMatchOutcome.ConfirmedMatch.ToString(),
        "ProbableMatch" => PlaceMatchOutcome.ProbableMatch.ToString(),
        "New" or "NotMatch" => PlaceMatchOutcome.NotMatch.ToString(),
        _ => PlaceMatchOutcome.NeedsReview.ToString()
    };

    private static decimal MatchScore(PlaceMatchDecision match) => match.Band switch
    {
        "ExactExternalIdentity" => 100, "ConfirmedMatch" => 95, "ProbableMatch" => 75, "NeedsReview" => 50, _ => 0
    };

    private static PlaceImportDecisionRecord Decision(Guid runId, Guid actorId, string action, string reason, DateTimeOffset now) =>
        new() { Id = Guid.NewGuid(), ImportRunId = runId, ActorId = actorId, Action = action, Reason = reason, DecidedAtUtc = now };

    private async Task LinkObservations(Guid importId, Guid sourceRecordId, Guid placeId, CancellationToken ct)
    {
        var observations = await db.PlaceFieldObservations
            .Where(x => x.ImportRunId == importId && x.SourceRecordId == sourceRecordId).ToArrayAsync(ct);
        foreach (var observation in observations)
        {
            observation.PlaceId = placeId;
            observation.VerificationStatus = "Verified";
            observation.VerifiedAtUtc ??= DateTimeOffset.UtcNow;
        }
    }

    private static Place BuildPlace(PlaceImportItemRecord item, IReadOnlyDictionary<string,string> fields, PlaceImportRunRecord run)
    {
        var completeAddress = fields.GetValueOrDefault("address.complete")?.Trim();
        var street = fields.GetValueOrDefault("address.street")?.Trim();
        var houseNumber = fields.GetValueOrDefault("address.housenumber")?.Trim();
        var address = string.Join(" ", new[] { street, houseNumber }.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (string.IsNullOrWhiteSpace(address)) address = completeAddress ?? "Adreça no informada per la font";
        var (status, species) = PetPolicy(fields);
        var type = PlaceCommercialClassification.GeneralType(item.Category);
        var place = new Place(Guid.NewGuid(), item.Name,
            type,
            $"{item.Category.Replace('_', ' ')} a {run.TerritorialName}.",
            $"Informació procedent de {run.Source.Dataset}. Confirma la política pet-friendly abans d’anar-hi.", string.Empty,
            new PostalAddress(address, run.TerritorialName, run.CountryIso2, fields.GetValueOrDefault("address.postcode") ?? string.Empty),
            new GeoLocation(item.Latitude!.Value, item.Longitude!.Value),
            new Zuppeto.Domain.Places.ValueObjects.PetPolicy(status, species, [],
                status == PetAccessStatus.Unknown ? "Informació pet-friendly desconeguda" : "Informació pet-friendly d’OpenStreetMap", string.Empty),
            new Pricing(string.Empty), new RatingSnapshot(0, 0),
            run.Source.SourceKey == OpenStreetMapSource.SourceKey ? PlaceDataProvenance.OpenStreetMap : PlaceDataProvenance.OpenData,
            territorialCountryId: run.CountryId, territorialUnitId: run.TerritorialUnitId,
            commercialCategory: item.Category);
        if (fields.GetValueOrDefault("amenity.outdoor_seating") == "Available")
            place.ReplaceFeatures(["Terrassa"]);
        return place;
    }

    private static (PetAccessStatus Status, IReadOnlyCollection<PetSpecies> Species) PetPolicy(IReadOnlyDictionary<string,string> fields)
    {
        var species = new List<PetSpecies>();
        var values = new List<string>();
        if (fields.TryGetValue("pet.dog", out var dog)) { values.Add(dog); if (dog is "Allowed" or "TerraceOnly") species.Add(PetSpecies.Dog); }
        if (fields.TryGetValue("pet.cat", out var cat)) { values.Add(cat); if (cat is "Allowed" or "TerraceOnly") species.Add(PetSpecies.Cat); }
        var status = values.Contains("Allowed") ? PetAccessStatus.Allowed
            : values.Contains("TerraceOnly") ? PetAccessStatus.TerraceOnly
            : values.Count > 0 && values.All(x => x == "NotAllowed") ? PetAccessStatus.NotAllowed
            : PetAccessStatus.Unknown;
        return (status, species);
    }

    private static bool IsExactOvertureBasicCategorySet(IReadOnlyCollection<string> categories)
    {
        string[] expected = ["restaurant", "cafe", "bar", "pub", "fast_food", "bakery", "ice_cream", "hotel", "hostel", "camping"];
        return categories.Count == expected.Length
            && categories.All(category => expected.Contains(category, StringComparer.OrdinalIgnoreCase));
    }

    private static PlaceImportRunDto ToDto(PlaceImportRunRecord x, string sourceKey) => new(x.Id, sourceKey, x.AccessMode,
        new(x.CountryId,x.CountryIso2,x.TerritorialUnitId,x.TerritorialName,x.OfficialCodeScheme,x.OfficialCode),
        JsonSerializer.Deserialize<string[]>(x.CategoriesJson, Json) ?? [], x.Status, x.Stage, x.SimulationOnly,
        x.DiscoveredCount,x.ValidCount,x.WarningCount,x.DuplicateCount,x.BoundaryExternalId,x.CreatedAtUtc,x.UpdatedAtUtc,x.ErrorCode,x.ErrorMessage);
}
