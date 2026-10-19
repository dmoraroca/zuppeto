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
public sealed record PlaceDataOrganisationDto(Guid Id, string Key, string Name, string Type, string? CountryIso2,
    string OfficialWebsiteUrl, string Status, DateTimeOffset? VerifiedAtUtc);
public sealed record PlaceDataLicenseDto(Guid Id, string Code, string Name, string Version, string OfficialUrl,
    string CommercialUseStatus, string ModificationStatus, string RedistributionStatus,
    bool? AttributionRequired, bool? ShareAlikeRequired, string AdditionalRestrictions,
    string Status, DateTimeOffset? VerifiedAtUtc);
public sealed record PlaceDatasetLicenseEvidenceDto(Guid Id, Guid DatasetId, string Dataset, Guid LicenseId,
    string LicenseCode, string TermsUrl, DateTimeOffset CheckedAtUtc, string Evidence,
    string VerificationStatus, string Notes, bool IsCurrent);
public sealed record PlaceDatasetSourceDto(Guid Id, string SourceKey, string Organisation, string Dataset,
    string DatasetVersion, string Coverage, string DatasetUrl, string LicenseIdentifier, string LicenseUrl,
    string Attribution, bool CommercialUseAllowed, bool PermanentStorageAllowed, string ReuseStatus,
    string ApprovalStatus, DateTimeOffset ReviewedAtUtc, DateTimeOffset? ConsultedAtUtc);
public sealed record PlaceTraceabilityFieldDto(Guid Id, string FieldName, string OriginalValue,
    string NormalizedValue, string Authority, string State, decimal? Confidence,
    string VerificationStatus, DateTimeOffset ObservedAtUtc, DateTimeOffset? VerifiedAtUtc);
public sealed record PlaceTraceabilitySourceDto(Guid Id, Guid DatasetId, string SourceKey, string Organisation,
    string Dataset, string DatasetUrl, string ExternalId, string RecordUrl, string? SourceVersion,
    string MatchStatus, string VerificationStatus, DateTimeOffset ObservedAtUtc,
    DateTimeOffset RetrievedAtUtc, DateTimeOffset? VerifiedAtUtc, string LicenseCode,
    string LicenseUrl, string CommercialUseStatus, string Attribution,
    IReadOnlyCollection<PlaceTraceabilityFieldDto> Fields);
public sealed record PlaceTraceabilityDto(Guid PlaceId, string PlaceName,
    IReadOnlyCollection<PlaceTraceabilitySourceDto> Sources, bool Complete);
public sealed record PlaceTraceabilitySummaryDto(int TotalPlaces, int PlacesWithSources,
    int PlacesWithFieldEvidence, int PlacesWithIncompleteProvenance, int PendingDatasets,
    int ProhibitedDatasets, int UnknownLicenseDatasets);
public sealed record PlaceSourceCheckpointDto(Guid Id, string SourceKey, string RegionKey, string Generation,
    string ExtractSha256, long? ReplicationSequence, string Status, string MetricsJson,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? CompletedAtUtc);
public sealed record PlaceCatalogGovernanceDto(IReadOnlyCollection<PlaceMatchReviewDto> Matches,
    IReadOnlyCollection<PlaceConflictReviewDto> Conflicts, IReadOnlyCollection<PlaceLifecycleReviewDto> Lifecycles,
    IReadOnlyCollection<PlaceDatasetSourceDto> Sources, IReadOnlyCollection<PlaceSourceCheckpointDto> Checkpoints,
    IReadOnlyCollection<PlaceDataOrganisationDto> Organisations, IReadOnlyCollection<PlaceDataLicenseDto> Licenses,
    IReadOnlyCollection<PlaceDatasetLicenseEvidenceDto> LicenseEvidence, PlaceTraceabilitySummaryDto Traceability);

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
    Task<PlaceTraceabilityDto> GetTraceabilityAsync(Guid placeId, CancellationToken cancellationToken = default);
    Task ResolveMatchAsync(Guid decisionId, PlaceMatchOutcome outcome, Guid? placeId, string reason, Guid actorId, CancellationToken cancellationToken = default);
    Task ResolveConflictAsync(Guid conflictId, Guid winningObservationId, string reason, Guid actorId, CancellationToken cancellationToken = default);
    Task ChangeLifecycleAsync(Guid placeId, PlaceLifecycleStatus status, IReadOnlyCollection<string> evidence, string reason, Guid actorId, CancellationToken cancellationToken = default);
    Task<Guid> RegisterDatasetAsync(RegisterOfficialDatasetRequest request, Guid actorId, CancellationToken cancellationToken = default);
    Task ReviewDatasetAsync(Guid sourceId, bool approved, string reason, Guid actorId, CancellationToken cancellationToken = default);
}

public interface IPlaceCatalogGovernanceService
{
    Task<PlaceCatalogGovernanceDto> GetAsync(CancellationToken cancellationToken = default);
    Task<PlaceTraceabilityDto> GetTraceabilityAsync(Guid placeId, CancellationToken cancellationToken = default);
    Task ResolveMatchAsync(Guid decisionId, ResolvePlaceMatchRequest request, Guid actorId, CancellationToken cancellationToken = default);
    Task ResolveConflictAsync(Guid conflictId, ResolvePlaceConflictRequest request, Guid actorId, CancellationToken cancellationToken = default);
    Task ChangeLifecycleAsync(Guid placeId, ChangePlaceLifecycleRequest request, Guid actorId, CancellationToken cancellationToken = default);
    Task<Guid> RegisterDatasetAsync(RegisterOfficialDatasetRequest request, Guid actorId, CancellationToken cancellationToken = default);
    Task ReviewDatasetAsync(Guid sourceId, ReviewOfficialDatasetRequest request, Guid actorId, CancellationToken cancellationToken = default);
}
