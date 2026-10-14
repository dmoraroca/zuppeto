using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

public sealed record PlaceMatchReviewDto(Guid Id, Guid SourceRecordId, string SourceKey, string ExternalId,
    Guid? CandidatePlaceId, string? CandidatePlaceName, string Outcome, decimal Score,
    IReadOnlyCollection<string> Signals, IReadOnlyCollection<string> Contradictions, bool IsCurrent,
    string Reason, DateTimeOffset DecidedAtUtc);
public sealed record PlaceConflictReviewDto(Guid Id, Guid PlaceId, string PlaceName, string FieldName,
    Guid LeftObservationId, string LeftValue, string LeftAuthority, string LeftSource,
    Guid RightObservationId, string RightValue, string RightAuthority, string RightSource,
    string Status, string ResolutionReason, DateTimeOffset DetectedAtUtc);
public sealed record PlaceLifecycleReviewDto(Guid PlaceId, string PlaceName, string Status,
    IReadOnlyCollection<PlaceLifecycleEventDto> History);
public sealed record PlaceLifecycleEventDto(Guid Id, string FromStatus, string ToStatus,
    IReadOnlyCollection<string> Evidence, bool Automatic, string Reason, DateTimeOffset CreatedAtUtc);
public sealed record PlaceDatasetSourceDto(Guid Id, string SourceKey, string Organisation, string Dataset,
    string DatasetVersion, string Coverage, string LicenseIdentifier, string Attribution,
    bool CommercialUseAllowed, bool PermanentStorageAllowed, string ApprovalStatus, DateTimeOffset ReviewedAtUtc);
public sealed record PlaceCatalogGovernanceDto(IReadOnlyCollection<PlaceMatchReviewDto> Matches,
    IReadOnlyCollection<PlaceConflictReviewDto> Conflicts, IReadOnlyCollection<PlaceLifecycleReviewDto> Lifecycles,
    IReadOnlyCollection<PlaceDatasetSourceDto> Sources);

public sealed record ResolvePlaceMatchRequest(string Outcome, Guid? PlaceId, string Reason);
public sealed record ResolvePlaceConflictRequest(Guid WinningObservationId, string Reason);
public sealed record ChangePlaceLifecycleRequest(string Status, IReadOnlyCollection<string> Evidence, string Reason);
public sealed record RegisterOfficialDatasetRequest(string SourceKey, string Organisation, string Dataset,
    string DatasetVersion, string Coverage, string Url, string AttributionUrl, string LicenseIdentifier,
    string LicenseUrl, string Attribution, string ReuseConditions, bool CommercialUseAllowed,
    bool PermanentStorageAllowed);
public sealed record ReviewOfficialDatasetRequest(bool Approved, string Reason);

public interface IPlaceCatalogGovernanceRepository
{
    Task<PlaceCatalogGovernanceDto> GetAsync(CancellationToken cancellationToken = default);
    Task ResolveMatchAsync(Guid decisionId, PlaceMatchOutcome outcome, Guid? placeId, string reason, Guid actorId, CancellationToken cancellationToken = default);
    Task ResolveConflictAsync(Guid conflictId, Guid winningObservationId, string reason, Guid actorId, CancellationToken cancellationToken = default);
    Task ChangeLifecycleAsync(Guid placeId, PlaceLifecycleStatus status, IReadOnlyCollection<string> evidence, string reason, Guid actorId, CancellationToken cancellationToken = default);
    Task<Guid> RegisterDatasetAsync(RegisterOfficialDatasetRequest request, Guid actorId, CancellationToken cancellationToken = default);
    Task ReviewDatasetAsync(Guid sourceId, bool approved, string reason, Guid actorId, CancellationToken cancellationToken = default);
}

public interface IPlaceCatalogGovernanceService
{
    Task<PlaceCatalogGovernanceDto> GetAsync(CancellationToken cancellationToken = default);
    Task ResolveMatchAsync(Guid decisionId, ResolvePlaceMatchRequest request, Guid actorId, CancellationToken cancellationToken = default);
    Task ResolveConflictAsync(Guid conflictId, ResolvePlaceConflictRequest request, Guid actorId, CancellationToken cancellationToken = default);
    Task ChangeLifecycleAsync(Guid placeId, ChangePlaceLifecycleRequest request, Guid actorId, CancellationToken cancellationToken = default);
    Task<Guid> RegisterDatasetAsync(RegisterOfficialDatasetRequest request, Guid actorId, CancellationToken cancellationToken = default);
    Task ReviewDatasetAsync(Guid sourceId, ReviewOfficialDatasetRequest request, Guid actorId, CancellationToken cancellationToken = default);
}
