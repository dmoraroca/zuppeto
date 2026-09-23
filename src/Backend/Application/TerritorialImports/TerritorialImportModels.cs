using Zuppeto.Domain.TerritorialImports;

namespace Zuppeto.Application.TerritorialImports;

public sealed record TerritorialSourceWorkbook(IReadOnlyCollection<TerritorialSourceSheet> Sheets, string SchemaFingerprint);
public sealed record TerritorialSourceSheet(string Name, IReadOnlyCollection<TerritorialSourceRow> Rows);
public sealed record TerritorialSourceRow(int Number, IReadOnlyDictionary<string, string?> Values);

public enum MappingValueOperation { Column, Constant, Concat, Coalesce }
public enum MappingConditionOperation { Equals, NotEquals, In, NotIn, NotEmpty }

public sealed record MappingValueDefinition(
    MappingValueOperation Operation,
    string? Column = null,
    string? Constant = null,
    IReadOnlyCollection<MappingValueDefinition>? Parts = null,
    string Separator = "",
    IReadOnlyCollection<string>? NullValues = null);

public sealed record MappingConditionDefinition(
    string Column,
    MappingConditionOperation Operation,
    string? Value = null,
    IReadOnlyCollection<string>? Values = null);

public sealed record TerritorialCodeMappingDefinition(string Scheme, MappingValueDefinition Value, bool IsPrimary = false);

public sealed record TerritorialUnitProjectionDefinition(
    string TerritorialUnitTypeCode,
    MappingValueDefinition CanonicalUnitKey,
    MappingValueDefinition? ParentCanonicalUnitKey,
    MappingValueDefinition Name,
    string? Locale,
    string NameKind,
    IReadOnlyCollection<TerritorialCodeMappingDefinition> Codes,
    MappingValueDefinition? Longitude = null,
    MappingValueDefinition? Latitude = null,
    bool ZeroZeroIsSentinel = false,
    IReadOnlyCollection<MappingConditionDefinition>? Conditions = null);

public sealed record TerritorialSheetMappingDefinition(
    string Sheet,
    int HeaderRow,
    IReadOnlyCollection<TerritorialUnitProjectionDefinition> Units);

public sealed record DeactivationGuardDefinition(int MaximumCount, decimal MaximumPercentage);

public sealed record TerritorialMappingDefinition(
    IReadOnlyCollection<TerritorialSheetMappingDefinition> Sheets,
    string? Canonicalizer = null,
    DeactivationGuardDefinition? DeactivationGuard = null);

public sealed record CanonicalTerritorialName(string Name, string? Locale, string Kind, bool IsPrimary = true, Guid? DatasetSourceId = null);
public sealed record CanonicalTerritorialCode(
    string Scheme,
    string Value,
    bool IsPrimary = false,
    DateOnly? ValidFrom = null,
    DateOnly? ValidTo = null,
    Guid? DatasetSourceId = null);

public sealed record CanonicalTerritorialUnit(
    string CanonicalUnitKey,
    string? ParentCanonicalUnitKey,
    string TerritorialUnitTypeCode,
    IReadOnlyCollection<CanonicalTerritorialName> Names,
    IReadOnlyCollection<CanonicalTerritorialCode> Codes,
    decimal? Latitude,
    decimal? Longitude,
    bool IsActive = true,
    Guid? CoordinateSourceId = null);

public sealed record TerritorialMappedRow(
    string Sheet,
    int RowNumber,
    IReadOnlyDictionary<string, string?> SourceValues,
    CanonicalTerritorialUnit Candidate);

public enum TerritorialIssueSeverity { Warning, Error }

public sealed record TerritorialImportIssue(
    string RuleCode,
    TerritorialIssueSeverity Severity,
    string Message,
    string? Sheet = null,
    int? RowNumber = null,
    string? Field = null,
    string? ProblemValue = null,
    string? CanonicalUnitKey = null);

public sealed record TerritorialCatalogUnitSnapshot(
    Guid Id,
    string TerritorialUnitTypeCode,
    Guid? ParentId,
    IReadOnlyCollection<CanonicalTerritorialName> Names,
    IReadOnlyCollection<CanonicalTerritorialCode> Codes,
    decimal? Latitude,
    decimal? Longitude,
    bool IsActive,
    Guid? CoordinateSourceId = null,
    bool HasManualActiveOverride = false,
    bool HasManualCoordinateOverride = false);

public sealed record TerritorialImportContext(
    Guid DatasetSourceId,
    Guid CountryId,
    string ApprovalStatus,
    string PublicationMode,
    long CatalogVersion,
    IReadOnlyCollection<string> TerritorialUnitTypeCodes,
    IReadOnlyCollection<TerritorialCatalogUnitSnapshot> CurrentUnits);

public sealed record PrepareTerritorialImportRequest(
    Guid DatasetSourceId,
    Guid MappingTemplateId,
    string ArtifactName,
    string DatasetVersion,
    Stream Artifact,
    Guid ActorUserId);

public sealed record PreparedTerritorialImport(
    TerritorialImport Import,
    IReadOnlyCollection<TerritorialMappedRow> Rows,
    IReadOnlyCollection<CanonicalTerritorialUnit> CanonicalUnits,
    IReadOnlyCollection<TerritorialImportIssue> Issues,
    TerritorialChangeSet? ChangeSet);

public interface ITerritorialWorkbookReader
{
    Task<TerritorialSourceWorkbook> ReadAsync(Stream source, CancellationToken cancellationToken = default);
}

public interface ITerritorialImportAuthorizer
{
    Task EnsureAdminAsync(Guid actorUserId, CancellationToken cancellationToken = default);
}

public interface ITerritorialImportStore
{
    Task<TerritorialMappingTemplate?> GetMappingAsync(Guid mappingTemplateId, CancellationToken cancellationToken = default);
    Task CreateAsync(TerritorialImport import, CancellationToken cancellationToken = default);
    Task SaveMappedAsync(TerritorialImport import, IReadOnlyCollection<TerritorialMappedRow> rows, CancellationToken cancellationToken = default);
    Task SaveValidatedAsync(TerritorialImport import, IReadOnlyCollection<TerritorialImportIssue> issues, CancellationToken cancellationToken = default);
    Task SaveReadyAsync(TerritorialImport import, TerritorialChangeSet changeSet, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid importId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(TerritorialImport import, CancellationToken cancellationToken = default);
}

public interface ITerritorialCatalogImportGateway
{
    Task<TerritorialImportContext> GetContextAsync(Guid datasetSourceId, CancellationToken cancellationToken = default);
    Task PublishAsync(Guid importId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task RevertLatestAsync(Guid importId, Guid actorUserId, CancellationToken cancellationToken = default);
}

public interface ITerritorialCanonicalizer
{
    string Key { get; }
    (IReadOnlyCollection<CanonicalTerritorialUnit> Units, IReadOnlyCollection<TerritorialImportIssue> Issues) Canonicalize(
        IReadOnlyCollection<TerritorialMappedRow> rows);
}
