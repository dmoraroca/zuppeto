using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Zuppeto.Application.TerritorialImports;

public sealed class TerritorialAdminService(
    ITerritorialImportAuthorizer authorizer,
    ITerritorialWorkbookReader reader,
    ITerritorialAdminRepository repository,
    TerritorialImportService importService,
    IEnumerable<ITerritorialCanonicalizer> canonicalizers)
{
    public const long MaximumArtifactSize = 25 * 1024 * 1024;

    public async Task<TerritorialAdminContextDto> GetContextAsync(Guid actorUserId, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        return await repository.GetContextAsync(ct);
    }

    public async Task<TerritorialWorkbookInspectionDto> InspectAsync(
        Guid actorUserId,
        Guid datasetSourceId,
        string artifactName,
        long fileSize,
        Stream artifact,
        CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        ValidateArtifact(artifactName, fileSize);
        await using var buffer = new MemoryStream();
        await artifact.CopyToAsync(buffer, ct);
        if (buffer.Length != fileSize) throw new InvalidDataException("La mida rebuda no coincideix amb la declarada.");
        var bytes = buffer.ToArray();
        buffer.Position = 0;
        var workbook = await reader.ReadAsync(buffer, ct);
        var mappings = await repository.ListMappingsAsync(datasetSourceId, workbook.SchemaFingerprint, ct);
        return new TerritorialWorkbookInspectionDto(
            SafeArtifactName(artifactName),
            fileSize,
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            "XLSX",
            workbook.SchemaFingerprint,
            workbook.Sheets.Select(ToSheetMetadata).ToArray(),
            workbook.Sheets.SelectMany(sheet => sheet.Rows.Skip(1).Take(20).Select(row =>
                new TerritorialWorkbookPreviewRowDto(sheet.Name, row.Number, row.Values))).ToArray(),
            mappings);
    }

    public async Task<IReadOnlyCollection<TerritorialMappingTemplateSummaryDto>> ListMappingsAsync(
        Guid actorUserId, Guid? datasetSourceId, string? schemaFingerprint, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        return await repository.ListMappingsAsync(datasetSourceId, schemaFingerprint, ct);
    }

    public async Task<TerritorialMappingTemplateDetailDto?> GetMappingAsync(Guid actorUserId, Guid id, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        return await repository.GetMappingAsync(id, ct);
    }

    public async Task<TerritorialMappingTemplateDetailDto> CreateMappingAsync(
        Guid actorUserId, CreateTerritorialMappingTemplateRequest request, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        if (request.DatasetSourceId == Guid.Empty) throw new InvalidOperationException("La font del mapping és obligatòria.");
        if (request.SchemaFingerprint.Length != 64 || request.SchemaFingerprint.Any(ch => !Uri.IsHexDigit(ch)))
            throw new InvalidOperationException("El fingerprint de l'esquema no és vàlid.");
        var definitionJson = request.Definition.GetRawText();
        var definition = JsonSerializer.Deserialize<TerritorialMappingDefinition>(definitionJson, TerritorialImportJson.Options)
            ?? throw new InvalidOperationException("La definició del mapping no és vàlida.");
        ValidateDefinition(definition);
        var canonicalizer = definition.Canonicalizer ?? "default";
        if (!canonicalizers.Any(item => item.Key == canonicalizer))
            throw new InvalidOperationException("El canonicalitzador indicat no està disponible.");
        var normalized = JsonSerializer.Serialize(definition, TerritorialImportJson.Options);
        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
        return await repository.CreateMappingVersionAsync(request.DatasetSourceId, request.SchemaFingerprint.ToLowerInvariant(), normalized, checksum, ct);
    }

    public async Task<TerritorialImportDetailDto> PrepareAsync(
        Guid actorUserId,
        Guid datasetSourceId,
        Guid mappingTemplateId,
        string datasetVersion,
        string artifactName,
        long fileSize,
        Stream artifact,
        CancellationToken ct = default)
    {
        ValidateArtifact(artifactName, fileSize);
        if (string.IsNullOrWhiteSpace(datasetVersion) || datasetVersion.Length > 120)
            throw new InvalidOperationException("La versió del dataset és obligatòria i no pot superar 120 caràcters.");
        var importId = await importService.QueueAsync(new PrepareTerritorialImportRequest(
            datasetSourceId, mappingTemplateId, SafeArtifactName(artifactName), datasetVersion.Trim(), artifact, actorUserId), ct);
        return await repository.GetImportAsync(importId, ct)
            ?? throw new InvalidOperationException("No s'ha pogut recuperar la importació en cua.");
    }

    public async Task<PageResult<TerritorialImportListItemDto>> ListImportsAsync(Guid actorUserId, TerritorialImportQuery query, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        return await repository.ListImportsAsync(Normalize(query), ct);
    }

    public async Task<TerritorialImportDetailDto?> GetImportAsync(Guid actorUserId, Guid id, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        return await repository.GetImportAsync(id, ct);
    }

    public async Task<PageResult<TerritorialImportIssueDto>> ListIssuesAsync(Guid actorUserId, Guid importId, TerritorialIssueQuery query, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        return await repository.ListIssuesAsync(importId, Normalize(query), ct);
    }

    public async Task<PageResult<TerritorialChangeItemDto>> ListChangesAsync(Guid actorUserId, Guid importId, TerritorialChangeQuery query, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        return await repository.ListChangesAsync(importId, Normalize(query), ct);
    }

    public async Task<TerritorialChangeHierarchyDto?> GetChangeHierarchyAsync(
        Guid actorUserId, Guid importId, Guid changeId, TerritorialChangeHierarchyQuery query, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        return await repository.GetChangeHierarchyAsync(importId, changeId, Normalize(query), ct);
    }

    public async Task<PageResult<TerritorialSourcePreviewRowDto>> ListSourcePreviewAsync(Guid actorUserId, Guid importId, TerritorialPreviewQuery query, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        return await repository.ListSourcePreviewAsync(importId, Normalize(query), ct);
    }

    public async Task<PageResult<TerritorialCanonicalPreviewRowDto>> ListCanonicalPreviewAsync(Guid actorUserId, Guid importId, TerritorialPreviewQuery query, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        return await repository.ListCanonicalPreviewAsync(importId, Normalize(query), ct);
    }

    public async Task<PageResult<TerritorialCatalogUnitDto>> ListCatalogAsync(Guid actorUserId, TerritorialCatalogQuery query, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        return await repository.ListCatalogAsync(Normalize(query), ct);
    }

    public async Task<TerritorialCatalogDetailDto?> GetCatalogUnitAsync(Guid actorUserId, Guid id, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        return await repository.GetCatalogUnitAsync(id, ct);
    }

    public async Task<TerritorialCatalogHierarchyDto?> GetCatalogHierarchyAsync(
        Guid actorUserId, Guid id, TerritorialCatalogHierarchyQuery query, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        return await repository.GetCatalogHierarchyAsync(id, Normalize(query), ct);
    }

    public async Task<PageResult<TerritorialCatalogUnitDto>> ListCatalogDescendantsAsync(
        Guid actorUserId, Guid id, TerritorialCatalogDescendantQuery query, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        return await repository.ListCatalogDescendantsAsync(id, Normalize(query), ct);
    }

    public async Task<TerritorialCatalogDetailDto> MaintainAsync(Guid actorUserId, Guid id, TerritorialMaintenanceRequest request, CancellationToken ct = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, ct);
        return await repository.MaintainAsync(id, actorUserId, request, ct);
    }

    public async Task<TerritorialImportDetailDto> PublishAsync(Guid actorUserId, Guid importId, CancellationToken ct = default)
    {
        await importService.PublishAsync(importId, actorUserId, ct);
        return await RequiredImport(importId, ct);
    }

    public async Task<TerritorialImportDetailDto> CancelAsync(Guid actorUserId, Guid importId, CancellationToken ct = default)
    {
        await importService.CancelAsync(importId, actorUserId, ct);
        return await RequiredImport(importId, ct);
    }

    public async Task<TerritorialImportDetailDto> RevertAsync(Guid actorUserId, Guid importId, CancellationToken ct = default)
    {
        await importService.RevertLatestAsync(importId, actorUserId, ct);
        return await RequiredImport(importId, ct);
    }

    private async Task<TerritorialImportDetailDto> RequiredImport(Guid id, CancellationToken ct) =>
        await repository.GetImportAsync(id, ct) ?? throw new KeyNotFoundException("No s'ha trobat la importació territorial.");

    private static TerritorialWorkbookSheetDto ToSheetMetadata(TerritorialSourceSheet sheet)
    {
        var header = sheet.Rows.Take(10).OrderByDescending(row => row.Values.Count(item => !string.IsNullOrWhiteSpace(item.Value))).FirstOrDefault();
        var columns = header?.Values.Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(item => new TerritorialWorkbookColumnDto(item.Key, item.Value!.Trim())).ToArray() ?? [];
        return new TerritorialWorkbookSheetDto(sheet.Name, header?.Number ?? 1, sheet.Rows.Count, columns);
    }

    private static void ValidateArtifact(string artifactName, long fileSize)
    {
        if (string.IsNullOrWhiteSpace(artifactName) || !string.Equals(Path.GetExtension(artifactName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Només s'admeten fitxers XLSX.");
        if (fileSize <= 0) throw new InvalidDataException("El fitxer és buit.");
        if (fileSize > MaximumArtifactSize) throw new InvalidDataException($"El fitxer supera el màxim de {MaximumArtifactSize / 1024 / 1024} MB.");
    }

    private static string SafeArtifactName(string name)
    {
        var safe = Path.GetFileName(name.Trim());
        if (safe.Length > 500) safe = safe[..500];
        return safe;
    }

    private static void ValidateDefinition(TerritorialMappingDefinition definition)
    {
        if (definition.Sheets.Count == 0 || definition.Sheets.Any(sheet => string.IsNullOrWhiteSpace(sheet.Sheet) || sheet.HeaderRow <= 0 || sheet.Units.Count == 0))
            throw new InvalidOperationException("El mapping ha de contenir almenys un full i una projecció territorial vàlida.");
        if (definition.Sheets.Select(item => item.Sheet).Distinct(StringComparer.Ordinal).Count() != definition.Sheets.Count)
            throw new InvalidOperationException("Un full no es pot declarar més d'una vegada al mapping.");
        if (definition.Sheets.SelectMany(item => item.Units).Any(unit => string.IsNullOrWhiteSpace(unit.TerritorialUnitTypeCode) || unit.Codes.Count == 0))
            throw new InvalidOperationException("Cada projecció necessita un tipus territorial i almenys un codi.");
    }

    private static TerritorialImportQuery Normalize(TerritorialImportQuery query) => query with { Page = Math.Max(1, query.Page), PageSize = Math.Clamp(query.PageSize, 1, 100) };
    private static TerritorialIssueQuery Normalize(TerritorialIssueQuery query) => query with { Page = Math.Max(1, query.Page), PageSize = Math.Clamp(query.PageSize, 1, 200) };
    private static TerritorialChangeQuery Normalize(TerritorialChangeQuery query) => query with { Page = Math.Max(1, query.Page), PageSize = Math.Clamp(query.PageSize, 1, 200) };
    private static TerritorialChangeHierarchyQuery Normalize(TerritorialChangeHierarchyQuery query) => query with { Page = Math.Max(1, query.Page), PageSize = Math.Clamp(query.PageSize, 1, 100) };
    private static TerritorialPreviewQuery Normalize(TerritorialPreviewQuery query) => query with { Page = Math.Max(1, query.Page), PageSize = Math.Clamp(query.PageSize, 1, 100) };
    private static TerritorialCatalogQuery Normalize(TerritorialCatalogQuery query) => query with { Page = Math.Max(1, query.Page), PageSize = Math.Clamp(query.PageSize, 1, 100) };
    private static TerritorialCatalogHierarchyQuery Normalize(TerritorialCatalogHierarchyQuery query) => query with { Page = Math.Max(1, query.Page), PageSize = Math.Clamp(query.PageSize, 1, 100) };
    private static TerritorialCatalogDescendantQuery Normalize(TerritorialCatalogDescendantQuery query) => query with { Page = Math.Max(1, query.Page), PageSize = Math.Clamp(query.PageSize, 1, 100) };
}

public sealed class TerritorialLocationService(ITerritorialLocationRepository repository)
{
    public Task<IReadOnlyCollection<TerritorialAdminCountryDto>> ListCountriesAsync(CancellationToken ct = default) =>
        repository.ListCountriesAsync(ct);

    public Task<PageResult<TerritorialLocalityOptionDto>> SearchLocalitiesAsync(Guid countryId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        if (countryId == Guid.Empty) throw new InvalidOperationException("El país és obligatori.");
        return repository.SearchLocalitiesAsync(countryId, search?.Trim(), Math.Max(1, page), Math.Clamp(pageSize, 1, 50), ct);
    }

    public async Task ValidateSelectionAsync(TerritorialLocationValidationRequest request, CancellationToken ct = default)
    {
        _ = await ResolveSelectionAsync(request.CountryId, request.TerritorialUnitId, ct);
    }

    public Task<TerritorialLocationSelectionDto> ResolveSelectionAsync(Guid countryId, Guid territorialUnitId, CancellationToken ct = default)
    {
        if (countryId == Guid.Empty || territorialUnitId == Guid.Empty)
            throw new InvalidOperationException("El país i la localitat són obligatoris.");
        return repository.ResolveSelectionAsync(countryId, territorialUnitId, ct);
    }
}
