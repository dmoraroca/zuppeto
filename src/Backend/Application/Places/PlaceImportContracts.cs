namespace Zuppeto.Application.Places;

public sealed record PreparePlaceImportRequest(
    Guid CountryId,
    Guid TerritorialUnitId,
    IReadOnlyCollection<string> Categories,
    string AccessMode,
    bool SimulationOnly = true);

public sealed record PlaceImportScope(
    Guid CountryId,
    string CountryIso2,
    Guid TerritorialUnitId,
    string TerritorialName,
    string OfficialCodeScheme,
    string OfficialCode);

public sealed record PlaceImportRunDto(
    Guid Id,
    string SourceKey,
    string AccessMode,
    PlaceImportScope Scope,
    IReadOnlyCollection<string> Categories,
    string Status,
    string Stage,
    bool SimulationOnly,
    int DiscoveredCount,
    int ValidCount,
    int WarningCount,
    int DuplicateCount,
    string? BoundaryExternalId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record PlaceImportItemDto(
    Guid Id,
    string ExternalId,
    string Name,
    string Category,
    decimal? Latitude,
    decimal? Longitude,
    string MatchBand,
    Guid? PlaceId,
    string Status,
    IReadOnlyDictionary<string, string> Fields);

public sealed record PlaceImportIssueDto(Guid Id, string Code, string Severity, string Message, string? ExternalId);

public sealed record PlaceImportDecisionDto(Guid Id, string Action, string Reason, Guid ActorId, DateTimeOffset DecidedAtUtc);

public sealed record PlaceImportDetailsDto(
    PlaceImportRunDto Run,
    IReadOnlyCollection<PlaceImportItemDto> Items,
    IReadOnlyCollection<PlaceImportIssueDto> Issues,
    IReadOnlyCollection<PlaceImportDecisionDto> History);

public sealed record PlaceImportWorkItem(Guid ImportId, string SourceKey, string AccessMode, PlaceSourceContext Context, string LeaseOwner);

public sealed record PlaceImportStagedItem(
    PlaceSourceDocument Source,
    NormalizedPlaceCandidate Candidate,
    PlaceMatchDecision Match,
    string ContentHash);

public interface IPlaceImportRepository
{
    Task<PlaceImportScope> ResolveScopeAsync(Guid countryId, Guid territorialUnitId, CancellationToken cancellationToken = default);
    Task<PlaceImportRunDto> CreateAsync(PreparePlaceImportRequest request, PlaceImportScope scope, Guid actorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PlaceImportRunDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PlaceImportDetailsDto?> GetAsync(Guid importId, CancellationToken cancellationToken = default);
    Task StageAsync(Guid importId, PlaceDiscoveryBatch batch, IReadOnlyCollection<PlaceImportStagedItem> items,
        IReadOnlyCollection<PlaceImportIssueDto> issues, CancellationToken cancellationToken = default);
    Task<bool> IsCancellationRequestedAsync(Guid importId, CancellationToken cancellationToken = default);
    Task CompleteCancellationAsync(Guid importId, CancellationToken cancellationToken = default);
    Task ApproveAsync(Guid importId, Guid actorId, string reason, CancellationToken cancellationToken = default);
    Task SimulatePublicationAsync(Guid importId, Guid actorId, string reason, CancellationToken cancellationToken = default);
    Task PublishAsync(Guid importId, Guid actorId, string reason, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid importId, Guid actorId, CancellationToken cancellationToken = default);
}

public interface IPlaceImportWorkQueue
{
    Task<PlaceImportWorkItem?> ClaimNextAsync(string owner, TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task HeartbeatAsync(Guid importId, string owner, TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task CompleteAsync(Guid importId, string owner, CancellationToken cancellationToken = default);
    Task FailAsync(Guid importId, string owner, string code, string message, bool recoverable, int maximumAttempts, CancellationToken cancellationToken = default);
}

public interface IPlaceImportApplicationService
{
    Task<PlaceImportRunDto> PrepareAsync(PreparePlaceImportRequest request, Guid actorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PlaceImportRunDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PlaceImportDetailsDto?> GetAsync(Guid importId, CancellationToken cancellationToken = default);
    Task ApproveAsync(Guid importId, string reason, Guid actorId, CancellationToken cancellationToken = default);
    Task SimulatePublicationAsync(Guid importId, string reason, Guid actorId, CancellationToken cancellationToken = default);
    Task PublishAsync(Guid importId, string reason, Guid actorId, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid importId, Guid actorId, CancellationToken cancellationToken = default);
}
