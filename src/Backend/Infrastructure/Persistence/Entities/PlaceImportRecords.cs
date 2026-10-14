namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class PlaceDatasetSourceRecord
{
    public Guid Id { get; set; }
    public string SourceKey { get; set; } = string.Empty;
    public string Organisation { get; set; } = string.Empty;
    public string Dataset { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string AttributionUrl { get; set; } = string.Empty;
    public string LicenseIdentifier { get; set; } = string.Empty;
    public string LicenseUrl { get; set; } = string.Empty;
    public string Attribution { get; set; } = string.Empty;
    public string ReuseConditions { get; set; } = string.Empty;
    public bool PermanentStorageAllowed { get; set; }
    public bool OdblDerived { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public DateTimeOffset ReviewedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class PlaceImportRunRecord
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public PlaceDatasetSourceRecord Source { get; set; } = null!;
    public Guid CountryId { get; set; }
    public CountryRecord Country { get; set; } = null!;
    public Guid TerritorialUnitId { get; set; }
    public TerritorialUnitRecord TerritorialUnit { get; set; } = null!;
    public string CountryIso2 { get; set; } = string.Empty;
    public string TerritorialName { get; set; } = string.Empty;
    public string OfficialCodeScheme { get; set; } = string.Empty;
    public string OfficialCode { get; set; } = string.Empty;
    public string CategoriesJson { get; set; } = "[]";
    public string AccessMode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public bool SimulationOnly { get; set; }
    public string? BoundaryExternalId { get; set; }
    public int DiscoveredCount { get; set; }
    public int ValidCount { get; set; }
    public int WarningCount { get; set; }
    public int DuplicateCount { get; set; }
    public Guid CreatedByActorId { get; set; }
    public UserRecord CreatedByActor { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? ProcessingStartedAtUtc { get; set; }
    public DateTimeOffset? ProcessingCompletedAtUtc { get; set; }
    public bool CancellationRequested { get; set; }
    public string? LeaseOwner { get; set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; set; }
    public DateTimeOffset? LastHeartbeatAtUtc { get; set; }
    public DateTimeOffset? NextAttemptAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsRecoverable { get; set; }
}

public sealed class PlaceSourceRecord
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public PlaceDatasetSourceRecord Source { get; set; } = null!;
    public string ExternalId { get; set; } = string.Empty;
    public string? SourceVersion { get; set; }
    public string NormalizedPayloadJson { get; set; } = "{}";
    public string ContentHash { get; set; } = string.Empty;
    public string EvidenceUri { get; set; } = string.Empty;
    public DateTimeOffset ObservedAtUtc { get; set; }
    public DateTimeOffset RetrievedAtUtc { get; set; }
    public DateTimeOffset FirstSeenAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; }
    public string State { get; set; } = string.Empty;
    public Guid? CanonicalPlaceId { get; set; }
    public PlaceRecord? CanonicalPlace { get; set; }
}

public sealed class PlaceFieldObservationRecord
{
    public Guid Id { get; set; }
    public Guid SourceRecordId { get; set; }
    public PlaceSourceRecord SourceRecord { get; set; } = null!;
    public Guid ImportRunId { get; set; }
    public PlaceImportRunRecord ImportRun { get; set; } = null!;
    public Guid? PlaceId { get; set; }
    public PlaceRecord? Place { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string OriginalValue { get; set; } = string.Empty;
    public string NormalizedValue { get; set; } = string.Empty;
    public string LicenseIdentifier { get; set; } = string.Empty;
    public string Attribution { get; set; } = string.Empty;
    public string Authority { get; set; } = "AUTO";
    public string State { get; set; } = "Current";
    public DateTimeOffset ObservedAtUtc { get; set; }
    public DateTimeOffset? ValidUntilUtc { get; set; }
}

public sealed class PlaceImportItemRecord
{
    public Guid Id { get; set; }
    public Guid ImportRunId { get; set; }
    public PlaceImportRunRecord ImportRun { get; set; } = null!;
    public Guid SourceRecordId { get; set; }
    public PlaceSourceRecord SourceRecord { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string NormalizedFieldsJson { get; set; } = "{}";
    public string MatchBand { get; set; } = string.Empty;
    public string MatchSignalsJson { get; set; } = "[]";
    public string MatchContradictionsJson { get; set; } = "[]";
    public Guid? PlaceId { get; set; }
    public PlaceRecord? Place { get; set; }
    public string Status { get; set; } = string.Empty;
    public string IdempotencyHash { get; set; } = string.Empty;
}

public sealed class PlaceImportIssueRecord
{
    public Guid Id { get; set; }
    public Guid ImportRunId { get; set; }
    public PlaceImportRunRecord ImportRun { get; set; } = null!;
    public string? ExternalId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class PlaceImportDecisionRecord
{
    public Guid Id { get; set; }
    public Guid ImportRunId { get; set; }
    public PlaceImportRunRecord ImportRun { get; set; } = null!;
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public UserRecord Actor { get; set; } = null!;
    public DateTimeOffset DecidedAtUtc { get; set; }
}
