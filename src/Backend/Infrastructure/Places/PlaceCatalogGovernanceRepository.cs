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
            x.SourceKey, x.Organisation, x.Dataset, x.DatasetVersion, x.Coverage, x.LicenseIdentifier, x.Attribution,
            x.CommercialUseAllowed, x.PermanentStorageAllowed, x.ApprovalStatus, x.ReviewedAtUtc)).ToArrayAsync(ct);
        var checkpoints = await db.PlaceSourceCheckpoints.AsNoTracking().Include(x => x.Source)
            .OrderByDescending(x => x.CreatedAtUtc).Take(100)
            .Select(x => new PlaceSourceCheckpointDto(x.Id, x.Source.SourceKey, x.RegionKey, x.Generation,
                x.ExtractSha256, x.ReplicationSequence, x.Status, x.MetricsJson, x.CreatedAtUtc, x.CompletedAtUtc))
            .ToArrayAsync(ct);
        return new(matches, conflicts, lifecycles, sources, checkpoints);
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
