using System.Text.Json;

namespace Zuppeto.Application.TerritorialImports;

public sealed record TerritorialAdminCountryDto(Guid Id, string Code, string Name, string? Iso2, string? Iso3, bool IsActive);

public sealed record TerritorialAdminSourceDto(
    Guid Id,
    Guid CountryId,
    string Organisation,
    string Dataset,
    string ApprovalStatus,
    bool IsActive,
    string PublicationMode,
    string? DatasetVersion,
    DateOnly? DatasetDate,
    string? License,
    string? Attribution);

public sealed record TerritorialAdminUnitTypeDto(Guid Id, Guid CountryId, string Code, string Name, int DisplayOrder, bool IsSelectableLocality);

public sealed record TerritorialAdminContextDto(
    IReadOnlyCollection<TerritorialAdminCountryDto> Countries,
    IReadOnlyCollection<TerritorialAdminSourceDto> Sources,
    IReadOnlyCollection<TerritorialAdminUnitTypeDto> UnitTypes);

public sealed record TerritorialWorkbookColumnDto(string Column, string Header);
public sealed record TerritorialWorkbookSheetDto(string Name, int DetectedHeaderRow, int RowCount, IReadOnlyCollection<TerritorialWorkbookColumnDto> Columns);
public sealed record TerritorialWorkbookInspectionDto(
    string ArtifactName,
    long FileSize,
    string FileChecksum,
    string Format,
    string SchemaFingerprint,
    IReadOnlyCollection<TerritorialWorkbookSheetDto> Sheets,
    IReadOnlyCollection<TerritorialMappingTemplateSummaryDto> CompatibleMappings);

public sealed record TerritorialMappingTemplateSummaryDto(
    Guid Id,
    Guid DatasetSourceId,
    int Version,
    string SchemaFingerprint,
    string DefinitionChecksum,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

public sealed record TerritorialMappingTemplateDetailDto(
    Guid Id,
    Guid DatasetSourceId,
    int Version,
    string SchemaFingerprint,
    string DefinitionChecksum,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    JsonElement Definition);

public sealed record CreateTerritorialMappingTemplateRequest(Guid DatasetSourceId, string SchemaFingerprint, JsonElement Definition);

public sealed record TerritorialImportCountersDto(int Rows, int Errors, int Warnings, int Create, int Update, int Deactivate, int NoChange);

public sealed record TerritorialImportListItemDto(
    Guid Id,
    Guid CountryId,
    string CountryName,
    Guid DatasetSourceId,
    string Organisation,
    string Dataset,
    string DatasetVersion,
    Guid MappingTemplateId,
    int MappingVersion,
    string Status,
    bool HasBlockingErrors,
    long? CatalogVersion,
    string ArtifactName,
    long FileSize,
    string FileChecksum,
    Guid CreatedByUserId,
    string Actor,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc,
    TerritorialImportCountersDto Counters);

public sealed record TerritorialImportDetailDto(
    TerritorialImportListItemDto Import,
    string PublicationMode,
    string? FailureReason,
    string SchemaFingerprint,
    bool CanCancel,
    bool CanPublish,
    bool CanRevert,
    long CurrentCatalogVersion,
    Guid? ChangeSetId,
    string? ChangeSetStatus,
    DateTimeOffset? ChangeSetCreatedAtUtc,
    DateTimeOffset? ChangeSetPublishedAtUtc);

public sealed record TerritorialImportIssueDto(
    Guid Id,
    string Severity,
    string RuleCode,
    string Message,
    string? Sheet,
    int? RowNumber,
    string? Field,
    string? ProblemValue,
    string? CanonicalUnitKey,
    DateTimeOffset CreatedAtUtc);

public sealed record TerritorialChangeItemDto(
    Guid Id,
    string Kind,
    Guid? TerritorialUnitId,
    string CanonicalUnitKey,
    JsonElement? Before,
    JsonElement? After,
    IReadOnlyCollection<string> ChangedFields);

public sealed record PageResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record TerritorialImportQuery(Guid? CountryId, Guid? DatasetSourceId, string? Status, int Page = 1, int PageSize = 25);
public sealed record TerritorialIssueQuery(string? Severity, string? RuleCode, string? Sheet, string? Field, int Page = 1, int PageSize = 50);
public sealed record TerritorialChangeQuery(string? Kind, string? Search, int Page = 1, int PageSize = 50);

public sealed record TerritorialPreviewQuery(string? Sheet, string? Search, int Page = 1, int PageSize = 50);
public sealed record TerritorialSourcePreviewRowDto(Guid Id, string Sheet, int RowNumber, JsonElement Values, string ReadingStatus);
public sealed record TerritorialCanonicalPreviewRowDto(
    Guid Id, string Sheet, int RowNumber, string CanonicalUnitKey, string? ParentCanonicalUnitKey,
    string Name, string? Locale, string TerritorialUnitTypeCode, IReadOnlyCollection<CanonicalTerritorialCode> Codes,
    decimal? Latitude, decimal? Longitude, string Status, int IssueCount);

public sealed record TerritorialCatalogQuery(
    Guid? CountryId, Guid? TerritorialUnitTypeId, string? Status, string? Locale, Guid? ParentId,
    string? Search, bool? SelectableLocality, int Page = 1, int PageSize = 50);
public sealed record TerritorialCatalogUnitDto(
    Guid Id, Guid CountryId, string Country, Guid TerritorialUnitTypeId, string TypeCode, string Type,
    Guid? ParentId, string? Parent, string? PrimaryCode, string PrimaryName, string? Locale,
    decimal? Latitude, decimal? Longitude, bool IsActive, bool IsSelectableLocality, bool HasManualOverride,
    bool HasManualActiveOverride, bool HasManualSelectableOverride, bool HasManualCoordinateOverride);
public sealed record TerritorialCatalogNameDto(Guid Id, string Name, string? Locale, string Kind, bool IsPrimary, string? Source);
public sealed record TerritorialCatalogCodeDto(Guid Id, string Scheme, string Value, DateOnly? ValidFrom, DateOnly? ValidTo, bool IsPrimary, string? Source);
public sealed record TerritorialAncestorDto(Guid Id, string Name, string Type);
public sealed record TerritorialMaintenanceAuditDto(
    Guid Id, string Action, string Field, string? BeforeValue, string? AfterValue, string Reason,
    Guid ActorUserId, string Actor, string Origin, DateTimeOffset CreatedAtUtc);
public sealed record TerritorialCatalogDetailDto(
    TerritorialCatalogUnitDto Unit, IReadOnlyCollection<TerritorialAncestorDto> Ancestors,
    IReadOnlyCollection<TerritorialCatalogNameDto> Names, IReadOnlyCollection<TerritorialCatalogCodeDto> Codes,
    string? CoordinateSource, IReadOnlyCollection<string> DatasetSources, IReadOnlyCollection<Guid> ImportIds,
    IReadOnlyCollection<TerritorialMaintenanceAuditDto> Audit);
public sealed record TerritorialMaintenanceRequest(string Action, string Reason, bool? IsSelectableLocality = null, decimal? Latitude = null, decimal? Longitude = null);

public sealed record TerritorialLocalityOptionDto(Guid Id, Guid CountryId, string Name, string Context, string? Locale);
public sealed record TerritorialLocationValidationRequest(Guid CountryId, Guid TerritorialUnitId);
public sealed record TerritorialLocationSelectionDto(Guid CountryId, string Country, Guid TerritorialUnitId, string Locality);

public interface ITerritorialAdminRepository
{
    Task<TerritorialAdminContextDto> GetContextAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TerritorialMappingTemplateSummaryDto>> ListMappingsAsync(Guid? datasetSourceId, string? schemaFingerprint, CancellationToken cancellationToken = default);
    Task<TerritorialMappingTemplateDetailDto?> GetMappingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TerritorialMappingTemplateDetailDto> CreateMappingVersionAsync(Guid datasetSourceId, string schemaFingerprint, string definitionJson, string definitionChecksum, CancellationToken cancellationToken = default);
    Task<PageResult<TerritorialImportListItemDto>> ListImportsAsync(TerritorialImportQuery query, CancellationToken cancellationToken = default);
    Task<TerritorialImportDetailDto?> GetImportAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PageResult<TerritorialImportIssueDto>> ListIssuesAsync(Guid importId, TerritorialIssueQuery query, CancellationToken cancellationToken = default);
    Task<PageResult<TerritorialChangeItemDto>> ListChangesAsync(Guid importId, TerritorialChangeQuery query, CancellationToken cancellationToken = default);
    Task<PageResult<TerritorialSourcePreviewRowDto>> ListSourcePreviewAsync(Guid importId, TerritorialPreviewQuery query, CancellationToken cancellationToken = default);
    Task<PageResult<TerritorialCanonicalPreviewRowDto>> ListCanonicalPreviewAsync(Guid importId, TerritorialPreviewQuery query, CancellationToken cancellationToken = default);
    Task<PageResult<TerritorialCatalogUnitDto>> ListCatalogAsync(TerritorialCatalogQuery query, CancellationToken cancellationToken = default);
    Task<TerritorialCatalogDetailDto?> GetCatalogUnitAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TerritorialCatalogDetailDto> MaintainAsync(Guid id, Guid actorUserId, TerritorialMaintenanceRequest request, CancellationToken cancellationToken = default);
}

public interface ITerritorialLocationRepository
{
    Task<IReadOnlyCollection<TerritorialAdminCountryDto>> ListCountriesAsync(CancellationToken cancellationToken = default);
    Task<PageResult<TerritorialLocalityOptionDto>> SearchLocalitiesAsync(Guid countryId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<TerritorialLocationSelectionDto> ResolveSelectionAsync(Guid countryId, Guid territorialUnitId, CancellationToken cancellationToken = default);
}
