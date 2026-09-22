namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class TerritorialMappingTemplateRecord
{
    public Guid Id { get; set; }
    public Guid DatasetSourceId { get; set; }
    public TerritorialDatasetSourceRecord DatasetSource { get; set; } = null!;
    public int Version { get; set; }
    public string DefinitionJson { get; set; } = string.Empty;
    public string SchemaFingerprint { get; set; } = string.Empty;
    public string DefinitionChecksum { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class TerritorialImportRecord
{
    public Guid Id { get; set; }
    public Guid DatasetSourceId { get; set; }
    public TerritorialDatasetSourceRecord DatasetSource { get; set; } = null!;
    public Guid MappingTemplateId { get; set; }
    public TerritorialMappingTemplateRecord MappingTemplate { get; set; } = null!;
    public string ArtifactName { get; set; } = string.Empty;
    public string? ArtifactStorageKey { get; set; }
    public string FileChecksum { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string DatasetVersion { get; set; } = string.Empty;
    public string PublicationMode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool HasBlockingErrors { get; set; }
    public long? CatalogVersion { get; set; }
    public string SummaryJson { get; set; } = "{}";
    public string? FailureReason { get; set; }
    public Guid CreatedByUserId { get; set; }
    public UserRecord CreatedByUser { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public ICollection<TerritorialImportRowRecord> Rows { get; set; } = [];
    public ICollection<TerritorialImportIssueRecord> Issues { get; set; } = [];
    public ICollection<TerritorialChangeSetRecord> ChangeSets { get; set; } = [];
}

public sealed class TerritorialImportRowRecord
{
    public Guid Id { get; set; }
    public Guid ImportId { get; set; }
    public TerritorialImportRecord Import { get; set; } = null!;
    public string Sheet { get; set; } = string.Empty;
    public int RowNumber { get; set; }
    public string SourceJson { get; set; } = "{}";
    public string CanonicalJson { get; set; } = "{}";
    public string CanonicalUnitKey { get; set; } = string.Empty;
    public string? ParentCanonicalUnitKey { get; set; }
}

public sealed class TerritorialImportIssueRecord
{
    public Guid Id { get; set; }
    public Guid ImportId { get; set; }
    public TerritorialImportRecord Import { get; set; } = null!;
    public string RuleCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Sheet { get; set; }
    public int? RowNumber { get; set; }
    public string? Field { get; set; }
    public string? ProblemValue { get; set; }
    public string? CanonicalUnitKey { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class TerritorialCatalogStateRecord
{
    public Guid CountryId { get; set; }
    public CountryRecord Country { get; set; } = null!;
    public long Version { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class TerritorialChangeSetRecord
{
    public Guid Id { get; set; }
    public Guid ImportId { get; set; }
    public TerritorialImportRecord Import { get; set; } = null!;
    public Guid CountryId { get; set; }
    public CountryRecord Country { get; set; } = null!;
    public long CatalogVersion { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? RevertsChangeSetId { get; set; }
    public TerritorialChangeSetRecord? RevertsChangeSet { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public ICollection<TerritorialChangeSetItemRecord> Items { get; set; } = [];
}

public sealed class TerritorialChangeSetItemRecord
{
    public Guid Id { get; set; }
    public Guid ChangeSetId { get; set; }
    public TerritorialChangeSetRecord ChangeSet { get; set; } = null!;
    public string Kind { get; set; } = string.Empty;
    public Guid? TerritorialUnitId { get; set; }
    public string CanonicalUnitKey { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string ChangedFieldsJson { get; set; } = "[]";
}
