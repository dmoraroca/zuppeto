namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class ExternalIntelligenceRequestRecord
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Api { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string QueryHash { get; set; } = string.Empty;
    public string NormalizedRequestJson { get; set; } = "{}";
    public string? ResponseJson { get; set; }
    public Guid? MunicipalityId { get; set; }
    public Guid? PlaceId { get; set; }
    public string CategoriesJson { get; set; } = "[]";
    public string Status { get; set; } = string.Empty;
    public int? HttpStatus { get; set; }
    public string? FunctionalStatus { get; set; }
    public string VerificationStatus { get; set; } = "Unknown";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? ValidUntilUtc { get; set; }
    public long? DurationMs { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int CachedTokens { get; set; }
    public string ToolsJson { get; set; } = "[]";
    public decimal EstimatedCostUsd { get; set; }
    public decimal? ActualCostUsd { get; set; }
    public string SourcesJson { get; set; } = "[]";
    public Guid ProcessId { get; set; }
    public int RetryCount { get; set; }
    public string? ProviderRequestId { get; set; }
    public string? ErrorCode { get; set; }
    public string? BlockReason { get; set; }
    public bool Reused { get; set; }
    public Guid? ReusedFromRequestId { get; set; }
}

public sealed class ExternalIntelligenceCacheRecord
{
    public string QueryHash { get; set; } = string.Empty;
    public Guid CurrentRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ResponseJson { get; set; }
    public string SourcesJson { get; set; } = "[]";
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset ValidUntilUtc { get; set; }
    public bool IsNegative { get; set; }
    public decimal LastCostUsd { get; set; }
}

public sealed class ExternalIntelligenceEvidenceRecord
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid? PlaceId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? ProposedValueJson { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string? EvidenceText { get; set; }
    public DateTimeOffset ConsultedAtUtc { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public string VerificationStatus { get; set; } = "Unknown";
    public string? ReuseConditions { get; set; }
    public string? AuthorOrRightsHolder { get; set; }
    public string? License { get; set; }
    public string? LicenseUrl { get; set; }
    public bool? CommercialUsePermitted { get; set; }
    public bool? ModificationPermitted { get; set; }
    public string? AttributionRequirements { get; set; }
    public string AdminReviewStatus { get; set; } = "Pending";
}

public sealed class ExternalIntelligenceSearchRecord
{
    public Guid Id { get; set; }
    public string ViewerHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? MunicipalityId { get; set; }
    public string CategoriesJson { get; set; } = "[]";
    public string FiltersJson { get; set; } = "{}";
    public string QueryHash { get; set; } = string.Empty;
    public Guid? RequestId { get; set; }
    public int ResultCount { get; set; }
    public string DataOrigin { get; set; } = string.Empty;
    public bool Reused { get; set; }
    public bool ExternalExecuted { get; set; }
    public decimal AttributedCostUsd { get; set; }
}

public sealed class ExternalIntelligenceBudgetRecord
{
    public int Id { get; set; }
    public bool Enabled { get; set; }
    public bool EmergencyStop { get; set; } = true;
    public decimal DailyLimitUsd { get; set; } = 0.25m;
    public decimal MonthlyLimitUsd { get; set; } = 2m;
    public decimal MaximumRequestUsd { get; set; } = 0.05m;
    public bool PaidRetriesEnabled { get; set; }
    public int RawResponseRetentionDays { get; set; } = 30;
    public int SearchLogRetentionDays { get; set; } = 90;
    public string AlertThresholdsJson { get; set; } = "[50,80,95]";
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }
}
