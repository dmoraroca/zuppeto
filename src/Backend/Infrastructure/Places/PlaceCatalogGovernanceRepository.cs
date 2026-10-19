using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Common;
using Zuppeto.Domain.Places;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Places;

internal sealed class PlaceCatalogGovernanceRepository(ZuppetoDbContext db) : IPlaceCatalogGovernanceRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<PlaceCatalogGovernanceDto> GetAsync(CancellationToken ct = default)
    {
        var decisions = await db.PlaceMatchDecisions.AsNoTracking().Include(x => x.SourceRecord).ThenInclude(x => x.Source)
            .Include(x => x.CandidatePlace).Where(x => x.IsCurrent).OrderByDescending(x => x.DecidedAtUtc).Take(250).ToArrayAsync(ct);
        var matches = decisions.Select(x => new PlaceMatchReviewDto(x.Id, x.SourceRecordId, x.SourceRecord.Source.SourceKey,
            x.SourceRecord.ExternalId, x.CandidatePlaceId, x.CandidatePlace?.Name, x.Outcome, x.Score,
            ReadArray(x.SignalsJson), ReadArray(x.ContradictionsJson), x.IsCurrent, x.Reason, x.DecidedAtUtc)).ToArray();

        var conflictRecords = await db.PlaceSourceConflicts.AsNoTracking().Include(x => x.Place)
            .Include(x => x.LeftObservation).ThenInclude(x => x.SourceRecord).ThenInclude(x => x.Source)
            .Include(x => x.RightObservation).ThenInclude(x => x.SourceRecord).ThenInclude(x => x.Source)
            .OrderBy(x => x.Status).ThenByDescending(x => x.DetectedAtUtc).Take(250).ToArrayAsync(ct);
        var conflicts = conflictRecords.Select(x => new PlaceConflictReviewDto(x.Id, x.PlaceId, x.Place.Name, x.FieldName,
            x.LeftObservationId, x.LeftObservation.NormalizedValue, x.LeftObservation.Authority, x.LeftObservation.SourceRecord.Source.SourceKey,
            x.RightObservationId, x.RightObservation.NormalizedValue, x.RightObservation.Authority, x.RightObservation.SourceRecord.Source.SourceKey,
            x.Status, x.ResolutionReason, x.DetectedAtUtc)).ToArray();

        var places = await db.Places.AsNoTracking().OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToArrayAsync(ct);
        var events = await db.PlaceLifecycleEvents.AsNoTracking().OrderBy(x => x.CreatedAtUtc).ToArrayAsync(ct);
        var byPlace = events.GroupBy(x => x.PlaceId).ToDictionary(x => x.Key, x => x.ToArray());
        var lifecycles = places.Select(place =>
        {
            var history = byPlace.GetValueOrDefault(place.Id) ?? [];
            return new PlaceLifecycleReviewDto(place.Id, place.Name, history.LastOrDefault()?.ToStatus ?? PlaceLifecycleStatus.Active.ToString(),
                history.Select(x => new PlaceLifecycleEventDto(x.Id, x.FromStatus, x.ToStatus, ReadArray(x.EvidenceJson), x.Automatic, x.Reason, x.CreatedAtUtc)).ToArray());
        }).ToArray();
        var sources = await db.PlaceDatasetSources.AsNoTracking().OrderBy(x => x.SourceKey).Select(x => new PlaceDatasetSourceDto(x.Id,
            x.SourceKey, x.Organisation, x.Dataset, x.DatasetVersion, x.Coverage, x.Url, x.LicenseIdentifier,
            x.LicenseUrl, x.Attribution, x.CommercialUseAllowed, x.PermanentStorageAllowed, x.ReuseStatus,
            x.ApprovalStatus, x.ReviewedAtUtc, x.ConsultedAtUtc)).ToArrayAsync(ct);
        var checkpoints = await db.PlaceSourceCheckpoints.AsNoTracking().Include(x => x.Source)
            .OrderByDescending(x => x.CreatedAtUtc).Take(100)
            .Select(x => new PlaceSourceCheckpointDto(x.Id, x.Source.SourceKey, x.RegionKey, x.Generation,
                x.ExtractSha256, x.ReplicationSequence, x.Status, x.MetricsJson, x.CreatedAtUtc, x.CompletedAtUtc))
            .ToArrayAsync(ct);
        var organisations = await db.PlaceDataOrganisations.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new PlaceDataOrganisationDto(x.Id, x.OrganisationKey, x.Name, x.OrganisationType,
                x.CountryIso2, x.OfficialWebsiteUrl, x.Status, x.VerifiedAtUtc)).ToArrayAsync(ct);
        var licenses = await db.PlaceDataLicenses.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new PlaceDataLicenseDto(x.Id, x.Code, x.Name, x.Version, x.OfficialUrl,
                x.CommercialUseStatus, x.ModificationStatus, x.RedistributionStatus,
                x.AttributionRequired, x.ShareAlikeRequired, x.AdditionalRestrictions,
                x.Status, x.VerifiedAtUtc)).ToArrayAsync(ct);
        var licenseEvidence = await db.PlaceDatasetLicenseEvidence.AsNoTracking()
            .Include(x => x.Source).Include(x => x.License).OrderBy(x => x.Source.SourceKey)
            .ThenByDescending(x => x.CheckedAtUtc)
            .Select(x => new PlaceDatasetLicenseEvidenceDto(x.Id, x.SourceId, x.Source.Dataset,
                x.LicenseId, x.License.Code, x.TermsUrl, x.CheckedAtUtc, x.Evidence,
                x.VerificationStatus, x.Notes, x.IsCurrent)).ToArrayAsync(ct);
        var totalPlaces = await db.Places.CountAsync(ct);
        var placesWithSources = await db.PlaceSourceRecords.Where(x => x.CanonicalPlaceId != null)
            .Select(x => x.CanonicalPlaceId).Distinct().CountAsync(ct);
        var placesWithEvidence = await db.PlaceFieldObservations.Where(x => x.PlaceId != null)
            .Select(x => x.PlaceId).Distinct().CountAsync(ct);
        var completePlaces = await db.Places.CountAsync(place =>
            db.PlaceSourceRecords.Any(source => source.CanonicalPlaceId == place.Id)
            && !db.PlaceSourceRecords.Any(source => source.CanonicalPlaceId == place.Id
                && (source.VerificationStatus != "Verified" || source.Source.ReuseStatus != "Allowed"
                    || source.Source.OrganisationId == null || source.Source.LicenseId == null))
            && db.PlaceFieldObservations.Any(field => field.PlaceId == place.Id
                && field.VerificationStatus == "Verified"), ct);
        var traceability = new PlaceTraceabilitySummaryDto(totalPlaces, placesWithSources, placesWithEvidence,
            totalPlaces - completePlaces,
            await db.PlaceDatasetSources.CountAsync(x => x.ApprovalStatus == "PendingReview", ct),
            await db.PlaceDatasetSources.CountAsync(x => x.ReuseStatus == "NotAllowed", ct),
            await db.PlaceDatasetSources.CountAsync(x => x.ReuseStatus == "Unknown" || x.LicenseId == null, ct));
        return new(matches, conflicts, lifecycles, sources, checkpoints, organisations, licenses,
            licenseEvidence, traceability);
    }

    public async Task<PlaceTraceabilityDto> GetTraceabilityAsync(Guid placeId, CancellationToken ct = default)
    {
        var place = await db.Places.AsNoTracking().Where(x => x.Id == placeId)
            .Select(x => new { x.Id, x.Name }).SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("El Place no existeix.");
        var records = await db.PlaceSourceRecords.AsNoTracking()
            .Include(x => x.Source).ThenInclude(x => x.LicenseRecord)
            .Where(x => x.CanonicalPlaceId == placeId).OrderBy(x => x.Source.SourceKey)
            .ThenBy(x => x.ExternalId).ToArrayAsync(ct);
        var recordIds = records.Select(x => x.Id).ToArray();
        var observations = await db.PlaceFieldObservations.AsNoTracking()
            .Where(x => x.PlaceId == placeId && recordIds.Contains(x.SourceRecordId))
            .OrderBy(x => x.FieldName).ThenByDescending(x => x.ObservedAtUtc).ToArrayAsync(ct);
        var byRecord = observations.GroupBy(x => x.SourceRecordId).ToDictionary(x => x.Key, x => x.ToArray());
        var sources = records.Select(record =>
        {
            var dataset = record.Source;
            var license = dataset.LicenseRecord;
            var fields = (byRecord.GetValueOrDefault(record.Id) ?? []).Select(x =>
                new PlaceTraceabilityFieldDto(x.Id, x.FieldName, x.OriginalValue, x.NormalizedValue,
                    x.Authority, x.State, x.Confidence, x.VerificationStatus, x.ObservedAtUtc,
                    x.VerifiedAtUtc)).ToArray();
            return new PlaceTraceabilitySourceDto(record.Id, dataset.Id, dataset.SourceKey,
                dataset.Organisation, dataset.Dataset, dataset.Url, record.ExternalId, record.EvidenceUri,
                record.SourceVersion, record.MatchStatus, record.VerificationStatus, record.ObservedAtUtc,
                record.RetrievedAtUtc, record.VerifiedAtUtc, license?.Code ?? dataset.LicenseIdentifier,
                license?.OfficialUrl ?? dataset.LicenseUrl, license?.CommercialUseStatus ?? "Unknown",
                dataset.Attribution, fields);
        }).ToArray();
        var complete = sources.Length > 0 && sources.All(x => x.VerificationStatus == "Verified"
            && x.CommercialUseStatus == "Allowed" && x.Fields.Count > 0);
        return new(place.Id, place.Name, sources, complete);
    }

    public async Task ResolveMatchAsync(Guid decisionId, PlaceMatchOutcome outcome, Guid? placeId, string reason, Guid actorId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var current = await db.PlaceMatchDecisions.Include(x => x.SourceRecord).SingleOrDefaultAsync(x => x.Id == decisionId && x.IsCurrent, ct)
            ?? throw new KeyNotFoundException("La decisió de matching no existeix o ja ha estat substituïda.");
        if (placeId is not null && !await db.Places.AnyAsync(x => x.Id == placeId, ct)) throw new KeyNotFoundException("El Place canònic no existeix.");
        var now = DateTimeOffset.UtcNow; current.IsCurrent = false; current.ReversedAtUtc = now;
        var confirmed = outcome == PlaceMatchOutcome.ConfirmedMatch;
        current.SourceRecord.CanonicalPlaceId = confirmed ? placeId : null;
        current.SourceRecord.MatchStatus = confirmed ? "Matched" : outcome.ToString();
        current.SourceRecord.VerificationStatus = confirmed ? "Verified" : "Pending";
        current.SourceRecord.VerifiedAtUtc = confirmed ? now : null;
        db.PlaceMatchDecisions.Add(new PlaceMatchDecisionRecord { Id = Guid.NewGuid(), SourceRecordId = current.SourceRecordId,
            CandidatePlaceId = placeId, Outcome = outcome.ToString(), Score = confirmed ? 100 : current.Score,
            SignalsJson = current.SignalsJson, ContradictionsJson = current.ContradictionsJson, IsCurrent = true,
            DecidedByActorId = actorId, Reason = reason, DecidedAtUtc = now });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }

    public async Task ResolveConflictAsync(Guid conflictId, Guid winningObservationId, string reason, Guid actorId, CancellationToken ct = default)
    {
        var conflict = await db.PlaceSourceConflicts.Include(x => x.LeftObservation).Include(x => x.RightObservation)
            .SingleOrDefaultAsync(x => x.Id == conflictId, ct) ?? throw new KeyNotFoundException("El conflicte no existeix.");
        if (conflict.Status != PlaceConflictStatus.Open.ToString()) throw new DomainRuleException("El conflicte ja està resolt.");
        var winner = winningObservationId == conflict.LeftObservationId ? conflict.LeftObservation
            : winningObservationId == conflict.RightObservationId ? conflict.RightObservation : throw new DomainRuleException("L’observació guanyadora no pertany al conflicte.");
        var loser = winner.Id == conflict.LeftObservationId ? conflict.RightObservation : conflict.LeftObservation;
        if (PlaceObservationAuthority.Rank(winner.Authority) < PlaceObservationAuthority.Rank(loser.Authority))
            throw new DomainRuleException("Una dada automàtica o manual no verificada no pot prevaler sobre una autoritat superior.");
        winner.State = "Current"; loser.State = "Superseded"; conflict.Status = PlaceConflictStatus.Resolved.ToString();
        conflict.WinningObservationId = winner.Id; conflict.ResolvedByActorId = actorId; conflict.ResolutionReason = reason; conflict.ResolvedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task ChangeLifecycleAsync(Guid placeId, PlaceLifecycleStatus status, IReadOnlyCollection<string> evidence, string reason, Guid actorId, CancellationToken ct = default)
    {
        if (!await db.Places.AnyAsync(x => x.Id == placeId, ct)) throw new KeyNotFoundException("El Place no existeix.");
        var current = await db.PlaceLifecycleEvents.AsNoTracking().Where(x => x.PlaceId == placeId).OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.ToStatus).FirstOrDefaultAsync(ct) ?? PlaceLifecycleStatus.Active.ToString();
        db.PlaceLifecycleEvents.Add(new PlaceLifecycleEventRecord { Id = Guid.NewGuid(), PlaceId = placeId, FromStatus = current,
            ToStatus = status.ToString(), EvidenceJson = JsonSerializer.Serialize(evidence, Json), Automatic = false,
            ActorId = actorId, Reason = reason, CreatedAtUtc = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> RegisterDatasetAsync(RegisterOfficialDatasetRequest request, Guid actorId, CancellationToken ct = default)
    {
        if (await db.PlaceDatasetSources.AnyAsync(x => x.SourceKey == request.SourceKey, ct)) throw new DomainRuleException("Ja existeix una font amb aquesta clau.");
        var source = new PlaceDatasetSourceRecord { Id = Guid.NewGuid(), SourceKey = request.SourceKey.Trim().ToLowerInvariant(),
            Organisation = request.Organisation.Trim(), Dataset = request.Dataset.Trim(), DatasetVersion = request.DatasetVersion.Trim(),
            Coverage = request.Coverage.Trim(), Url = request.Url.Trim(), AttributionUrl = request.AttributionUrl.Trim(),
            LicenseIdentifier = request.LicenseIdentifier.Trim(), LicenseUrl = request.LicenseUrl.Trim(), Attribution = request.Attribution.Trim(),
            ReuseConditions = request.ReuseConditions.Trim(), CommercialUseAllowed = request.CommercialUseAllowed,
            PermanentStorageAllowed = request.PermanentStorageAllowed, OdblDerived = false, ApprovalStatus = "PendingReview",
            ReviewedAtUtc = DateTimeOffset.UtcNow, CreatedAtUtc = DateTimeOffset.UtcNow };
        db.PlaceDatasetSources.Add(source); await db.SaveChangesAsync(ct); return source.Id;
    }

    public async Task ReviewDatasetAsync(Guid sourceId, bool approved, string reason, Guid actorId, CancellationToken ct = default)
    {
        var source = await db.PlaceDatasetSources.SingleOrDefaultAsync(x => x.Id == sourceId, ct) ?? throw new KeyNotFoundException("La font no existeix.");
        if (approved && (!source.CommercialUseAllowed || !source.PermanentStorageAllowed || string.IsNullOrWhiteSpace(source.LicenseIdentifier)))
            throw new DomainRuleException("La font no acredita ús comercial, persistència i llicència.");
        source.ApprovalStatus = approved ? "Approved" : "Rejected"; source.ReviewedAtUtc = DateTimeOffset.UtcNow;
        source.ReuseConditions = $"{source.ReuseConditions}\nRevisió ADMIN: {reason}".Trim(); await db.SaveChangesAsync(ct);
    }

    private static string[] ReadArray(string json) => JsonSerializer.Deserialize<string[]>(json, Json) ?? [];
}
