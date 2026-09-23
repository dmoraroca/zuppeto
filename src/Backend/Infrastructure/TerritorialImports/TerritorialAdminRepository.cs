using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.TerritorialImports;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.TerritorialImports;

internal sealed class TerritorialAdminRepository(ZuppetoDbContext db) : ITerritorialAdminRepository
{
    public async Task<TerritorialAdminContextDto> GetContextAsync(CancellationToken ct = default)
    {
        var countries = await db.Countries.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new TerritorialAdminCountryDto(x.Id, x.Code, x.Name, x.Iso2, x.Iso3, x.IsActive)).ToArrayAsync(ct);
        var sources = await db.TerritorialDatasetSources.AsNoTracking().OrderBy(x => x.Country.Name).ThenBy(x => x.Dataset)
            .Select(x => new TerritorialAdminSourceDto(x.Id, x.CountryId, x.Organisation, x.Dataset, x.ApprovalStatus,
                x.IsActive, x.PublicationMode, x.DatasetVersion, x.DatasetDate, x.License, x.Attribution)).ToArrayAsync(ct);
        var types = await db.TerritorialUnitTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.CountryId).ThenBy(x => x.DisplayOrder)
            .Select(x => new TerritorialAdminUnitTypeDto(x.Id, x.CountryId, x.Code, x.Name, x.DisplayOrder, x.IsSelectableLocality)).ToArrayAsync(ct);
        return new TerritorialAdminContextDto(countries, sources, types);
    }

    public async Task<IReadOnlyCollection<TerritorialMappingTemplateSummaryDto>> ListMappingsAsync(
        Guid? sourceId, string? fingerprint, CancellationToken ct = default)
    {
        var query = db.TerritorialMappingTemplates.AsNoTracking().AsQueryable();
        if (sourceId is not null) query = query.Where(x => x.DatasetSourceId == sourceId);
        if (!string.IsNullOrWhiteSpace(fingerprint)) query = query.Where(x => x.SchemaFingerprint == fingerprint.Trim().ToLower());
        return await query.OrderByDescending(x => x.Version)
            .Select(x => new TerritorialMappingTemplateSummaryDto(x.Id, x.DatasetSourceId, x.Version, x.SchemaFingerprint,
                x.DefinitionChecksum, x.IsActive, x.CreatedAtUtc)).ToArrayAsync(ct);
    }

    public async Task<TerritorialMappingTemplateDetailDto?> GetMappingAsync(Guid id, CancellationToken ct = default)
    {
        var record = await db.TerritorialMappingTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return record is null ? null : MappingDetail(record);
    }

    public async Task<TerritorialMappingTemplateDetailDto> CreateMappingVersionAsync(
        Guid sourceId, string fingerprint, string definitionJson, string checksum, CancellationToken ct = default)
    {
        if (!await db.TerritorialDatasetSources.AsNoTracking().AnyAsync(x => x.Id == sourceId, ct))
            throw new KeyNotFoundException("No s'ha trobat la font territorial.");
        var version = (await db.TerritorialMappingTemplates.Where(x => x.DatasetSourceId == sourceId)
            .MaxAsync(x => (int?)x.Version, ct) ?? 0) + 1;
        var record = new TerritorialMappingTemplateRecord
        {
            Id = Guid.NewGuid(), DatasetSourceId = sourceId, Version = version, DefinitionJson = definitionJson,
            SchemaFingerprint = fingerprint, DefinitionChecksum = checksum, IsActive = true, CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.TerritorialMappingTemplates.Add(record);
        await db.SaveChangesAsync(ct);
        return MappingDetail(record);
    }

    public async Task<PageResult<TerritorialImportListItemDto>> ListImportsAsync(TerritorialImportQuery request, CancellationToken ct = default)
    {
        var query = db.TerritorialImports.AsNoTracking().AsQueryable();
        if (request.CountryId is not null) query = query.Where(x => x.DatasetSource.CountryId == request.CountryId);
        if (request.DatasetSourceId is not null) query = query.Where(x => x.DatasetSourceId == request.DatasetSourceId);
        if (!string.IsNullOrWhiteSpace(request.Status)) query = query.Where(x => x.Status == request.Status.Trim());
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new TerritorialImportListItemDto(
                x.Id, x.DatasetSource.CountryId, x.DatasetSource.Country.Name, x.DatasetSourceId, x.DatasetSource.Organisation,
                x.DatasetSource.Dataset, x.DatasetVersion, x.MappingTemplateId, x.MappingTemplate.Version, x.Status,
                x.HasBlockingErrors, x.CatalogVersion, x.ArtifactName, x.FileSize, x.FileChecksum, x.CreatedByUserId,
                x.CreatedByUser.DisplayName ?? x.CreatedByUser.Email, x.CreatedAtUtc, x.UpdatedAtUtc, x.PublishedAtUtc,
                new TerritorialImportCountersDto(
                    x.Rows.Count,
                    x.Issues.Count(issue => issue.Severity == "Error"),
                    x.Issues.Count(issue => issue.Severity == "Warning"),
                    x.ChangeSets.Where(set => set.RevertsChangeSetId == null).SelectMany(set => set.Items).Count(item => item.Kind == "Create"),
                    x.ChangeSets.Where(set => set.RevertsChangeSetId == null).SelectMany(set => set.Items).Count(item => item.Kind == "Update"),
                    x.ChangeSets.Where(set => set.RevertsChangeSetId == null).SelectMany(set => set.Items).Count(item => item.Kind == "Deactivate"),
                    x.ChangeSets.Where(set => set.RevertsChangeSetId == null).SelectMany(set => set.Items).Count(item => item.Kind == "NoChange"))))
            .ToArrayAsync(ct);
        return new PageResult<TerritorialImportListItemDto>(rows, request.Page, request.PageSize, total);
    }

    public async Task<TerritorialImportDetailDto?> GetImportAsync(Guid id, CancellationToken ct = default)
    {
        var item = await db.TerritorialImports.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new
            {
                Summary = new TerritorialImportListItemDto(
                    x.Id, x.DatasetSource.CountryId, x.DatasetSource.Country.Name, x.DatasetSourceId, x.DatasetSource.Organisation,
                    x.DatasetSource.Dataset, x.DatasetVersion, x.MappingTemplateId, x.MappingTemplate.Version, x.Status,
                    x.HasBlockingErrors, x.CatalogVersion, x.ArtifactName, x.FileSize, x.FileChecksum, x.CreatedByUserId,
                    x.CreatedByUser.DisplayName ?? x.CreatedByUser.Email, x.CreatedAtUtc, x.UpdatedAtUtc, x.PublishedAtUtc,
                    new TerritorialImportCountersDto(
                        x.Rows.Count,
                        x.Issues.Count(issue => issue.Severity == "Error"),
                        x.Issues.Count(issue => issue.Severity == "Warning"),
                        x.ChangeSets.Where(set => set.RevertsChangeSetId == null).SelectMany(set => set.Items).Count(change => change.Kind == "Create"),
                        x.ChangeSets.Where(set => set.RevertsChangeSetId == null).SelectMany(set => set.Items).Count(change => change.Kind == "Update"),
                        x.ChangeSets.Where(set => set.RevertsChangeSetId == null).SelectMany(set => set.Items).Count(change => change.Kind == "Deactivate"),
                        x.ChangeSets.Where(set => set.RevertsChangeSetId == null).SelectMany(set => set.Items).Count(change => change.Kind == "NoChange"))),
                x.PublicationMode,
                x.FailureReason,
                x.MappingTemplate.SchemaFingerprint,
                CurrentCatalogVersion = db.TerritorialCatalogStates.Where(state => state.CountryId == x.DatasetSource.CountryId).Select(state => (long?)state.Version).FirstOrDefault() ?? 0,
                ChangeSet = x.ChangeSets.Where(set => set.RevertsChangeSetId == null).Select(set => new { set.Id, set.Status, set.CatalogVersion, set.CreatedAtUtc, set.PublishedAtUtc }).FirstOrDefault()
            }).SingleOrDefaultAsync(ct);
        if (item is null) return null;
        var changeSet = item.ChangeSet;
        var canRevert = item.Summary.Status == "Published" && changeSet is not null && item.CurrentCatalogVersion == changeSet.CatalogVersion + 1;
        return new TerritorialImportDetailDto(item.Summary, item.PublicationMode, item.FailureReason, item.SchemaFingerprint,
            item.Summary.Status is not ("Published" or "Reverted" or "Cancelled"),
            item.Summary.Status == "ReadyForReview" && !item.Summary.HasBlockingErrors,
            canRevert, item.CurrentCatalogVersion, changeSet?.Id, changeSet?.Status, changeSet?.CreatedAtUtc, changeSet?.PublishedAtUtc);
    }

    public async Task<PageResult<TerritorialImportIssueDto>> ListIssuesAsync(Guid importId, TerritorialIssueQuery request, CancellationToken ct = default)
    {
        var query = db.TerritorialImportIssues.AsNoTracking().Where(x => x.ImportId == importId);
        if (!string.IsNullOrWhiteSpace(request.Severity)) query = query.Where(x => x.Severity == request.Severity.Trim());
        if (!string.IsNullOrWhiteSpace(request.RuleCode)) query = query.Where(x => x.RuleCode == request.RuleCode.Trim());
        if (!string.IsNullOrWhiteSpace(request.Sheet)) query = query.Where(x => x.Sheet == request.Sheet.Trim());
        if (!string.IsNullOrWhiteSpace(request.Field)) query = query.Where(x => x.Field == request.Field.Trim());
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.Severity == "Error" ? 0 : 1).ThenBy(x => x.Sheet).ThenBy(x => x.RowNumber)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new TerritorialImportIssueDto(x.Id, x.Severity, x.RuleCode, x.Message, x.Sheet, x.RowNumber,
                x.Field, x.ProblemValue, x.CanonicalUnitKey, x.CreatedAtUtc)).ToArrayAsync(ct);
        return new PageResult<TerritorialImportIssueDto>(rows, request.Page, request.PageSize, total);
    }

    public async Task<PageResult<TerritorialChangeItemDto>> ListChangesAsync(Guid importId, TerritorialChangeQuery request, CancellationToken ct = default)
    {
        var query = db.TerritorialChangeSetItems.AsNoTracking()
            .Where(x => x.ChangeSet.ImportId == importId && x.ChangeSet.RevertsChangeSetId == null);
        if (!string.IsNullOrWhiteSpace(request.Kind)) query = query.Where(x => x.Kind == request.Kind.Trim());
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(x => x.CanonicalUnitKey.Contains(request.Search.Trim()));
        var total = await query.CountAsync(ct);
        var raw = await query.OrderBy(x => x.Kind == "Deactivate" ? 0 : x.Kind == "Update" ? 1 : x.Kind == "Create" ? 2 : 3)
            .ThenBy(x => x.CanonicalUnitKey).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new { x.Id, x.Kind, x.TerritorialUnitId, x.CanonicalUnitKey, x.BeforeJson, x.AfterJson, x.ChangedFieldsJson })
            .ToArrayAsync(ct);
        var rows = raw.Select(x => new TerritorialChangeItemDto(x.Id, x.Kind, x.TerritorialUnitId, x.CanonicalUnitKey,
            ParseOptional(x.BeforeJson), ParseOptional(x.AfterJson),
            JsonSerializer.Deserialize<string[]>(x.ChangedFieldsJson, TerritorialImportJson.Options) ?? [])).ToArray();
        return new PageResult<TerritorialChangeItemDto>(rows, request.Page, request.PageSize, total);
    }

    public async Task<PageResult<TerritorialSourcePreviewRowDto>> ListSourcePreviewAsync(Guid importId, TerritorialPreviewQuery request, CancellationToken ct = default)
    {
        var query = db.TerritorialImportRows.AsNoTracking().Where(x => x.ImportId == importId);
        if (!string.IsNullOrWhiteSpace(request.Sheet)) query = query.Where(x => x.Sheet == request.Sheet.Trim());
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(x => x.SourceJson.Contains(request.Search.Trim()));
        var total = await query.CountAsync(ct);
        var raw = await query.OrderBy(x => x.Sheet).ThenBy(x => x.RowNumber)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new { x.Id, x.Sheet, x.RowNumber, x.SourceJson }).ToArrayAsync(ct);
        var rows = raw.Select(x => new TerritorialSourcePreviewRowDto(
            x.Id, x.Sheet, x.RowNumber, JsonDocument.Parse(x.SourceJson).RootElement.Clone(), "Llegida")).ToArray();
        return new PageResult<TerritorialSourcePreviewRowDto>(rows, request.Page, request.PageSize, total);
    }

    public async Task<PageResult<TerritorialCanonicalPreviewRowDto>> ListCanonicalPreviewAsync(Guid importId, TerritorialPreviewQuery request, CancellationToken ct = default)
    {
        var query = db.TerritorialImportRows.AsNoTracking().Where(x => x.ImportId == importId);
        if (!string.IsNullOrWhiteSpace(request.Sheet)) query = query.Where(x => x.Sheet == request.Sheet.Trim());
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(x => x.CanonicalUnitKey.Contains(request.Search.Trim()) || x.CanonicalJson.Contains(request.Search.Trim()));
        var total = await query.CountAsync(ct);
        var raw = await query.OrderBy(x => x.Sheet).ThenBy(x => x.RowNumber)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new
            {
                x.Id, x.Sheet, x.RowNumber, x.CanonicalJson,
                IssueCount = db.TerritorialImportIssues.Count(issue => issue.ImportId == importId &&
                    issue.Sheet == x.Sheet && issue.RowNumber == x.RowNumber)
            }).ToArrayAsync(ct);
        var rows = raw.Select(x =>
        {
            var unit = JsonSerializer.Deserialize<CanonicalTerritorialUnit>(x.CanonicalJson, TerritorialImportJson.Options)!;
            var name = unit.Names.FirstOrDefault(item => item.IsPrimary) ?? unit.Names.FirstOrDefault();
            return new TerritorialCanonicalPreviewRowDto(x.Id, x.Sheet, x.RowNumber, unit.CanonicalUnitKey,
                unit.ParentCanonicalUnitKey, name?.Name ?? string.Empty, name?.Locale, unit.TerritorialUnitTypeCode,
                unit.Codes, unit.Latitude, unit.Longitude, x.IssueCount == 0 ? "Vàlida" : "Amb incidències", x.IssueCount);
        }).ToArray();
        return new PageResult<TerritorialCanonicalPreviewRowDto>(rows, request.Page, request.PageSize, total);
    }

    public async Task<PageResult<TerritorialCatalogUnitDto>> ListCatalogAsync(TerritorialCatalogQuery request, CancellationToken ct = default)
    {
        var query = db.TerritorialUnits.AsNoTracking().AsQueryable();
        if (request.CountryId is not null) query = query.Where(x => x.CountryId == request.CountryId);
        if (request.TerritorialUnitTypeId is not null) query = query.Where(x => x.TerritorialUnitTypeId == request.TerritorialUnitTypeId);
        if (request.ParentId is not null) query = query.Where(x => x.ParentId == request.ParentId);
        if (request.Status == "active") query = query.Where(x => x.IsActive);
        if (request.Status == "inactive") query = query.Where(x => !x.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Locale)) query = query.Where(x => x.Names.Any(n => n.Locale == request.Locale.Trim()));
        if (request.SelectableLocality is not null)
            query = request.SelectableLocality.Value
                ? query.Where(x => x.ManualSelectableLocality == true || (x.ManualSelectableLocality == null && x.TerritorialUnitType.IsSelectableLocality))
                : query.Where(x => x.ManualSelectableLocality == false || (x.ManualSelectableLocality == null && !x.TerritorialUnitType.IsSelectableLocality));
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var value = request.Search.Trim();
            query = query.Where(x => x.Names.Any(n => n.Name.Contains(value)) || x.Codes.Any(c => c.Value.Contains(value)));
        }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.Country.Name).ThenBy(x => x.TerritorialUnitType.DisplayOrder)
            .ThenBy(x => x.Names.Where(n => n.IsPrimary).Select(n => n.Name).FirstOrDefault())
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new TerritorialCatalogUnitDto(
                x.Id, x.CountryId, x.Country.Name, x.TerritorialUnitTypeId, x.TerritorialUnitType.Code, x.TerritorialUnitType.Name,
                x.ParentId, x.Parent == null ? null : x.Parent.Names.Where(n => n.IsPrimary).Select(n => n.Name).FirstOrDefault(),
                x.Codes.Where(c => c.IsPrimary).Select(c => c.Value).FirstOrDefault() ?? x.Codes.Select(c => c.Value).FirstOrDefault(),
                x.Names.Where(n => n.IsPrimary).Select(n => n.Name).FirstOrDefault() ?? x.Names.Select(n => n.Name).FirstOrDefault() ?? string.Empty,
                x.Names.Where(n => n.IsPrimary).Select(n => n.Locale).FirstOrDefault(),
                x.Latitude, x.Longitude, x.IsActive,
                x.ManualSelectableLocality ?? x.TerritorialUnitType.IsSelectableLocality,
                x.HasManualActiveOverride || x.ManualSelectableLocality != null || x.HasManualCoordinateOverride,
                x.HasManualActiveOverride, x.ManualSelectableLocality != null, x.HasManualCoordinateOverride)).ToArrayAsync(ct);
        return new PageResult<TerritorialCatalogUnitDto>(rows, request.Page, request.PageSize, total);
    }

    public async Task<TerritorialCatalogDetailDto?> GetCatalogUnitAsync(Guid id, CancellationToken ct = default)
    {
        var unit = await db.TerritorialUnits.AsNoTracking()
            .Include(x => x.Country).Include(x => x.TerritorialUnitType).Include(x => x.Parent).ThenInclude(x => x!.Names)
            .Include(x => x.Names).ThenInclude(x => x.DatasetSource)
            .Include(x => x.Codes).ThenInclude(x => x.DatasetSource)
            .Include(x => x.CoordinateSource)
            .Include(x => x.MaintenanceAudit).ThenInclude(x => x.ActorUser)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        if (unit is null) return null;
        var ancestors = new List<TerritorialAncestorDto>();
        var parentId = unit.ParentId;
        while (parentId is not null)
        {
            var parent = await db.TerritorialUnits.AsNoTracking().Where(x => x.Id == parentId)
                .Select(x => new { x.Id, x.ParentId, Name = x.Names.Where(n => n.IsPrimary).Select(n => n.Name).FirstOrDefault() ?? x.Names.Select(n => n.Name).FirstOrDefault() ?? string.Empty, Type = x.TerritorialUnitType.Name })
                .SingleAsync(ct);
            ancestors.Insert(0, new TerritorialAncestorDto(parent.Id, parent.Name, parent.Type));
            parentId = parent.ParentId;
        }
        var summary = await ListCatalogAsync(new TerritorialCatalogQuery(null, null, null, null, null, id.ToString(), null, 1, 1), ct);
        var dto = summary.Items.FirstOrDefault() ?? ToCatalogUnit(unit);
        var imports = await db.TerritorialChangeSetItems.AsNoTracking()
            .Where(x => x.TerritorialUnitId == id && x.ChangeSet.Status == "Published")
            .Select(x => x.ChangeSet.ImportId).Distinct().ToArrayAsync(ct);
        return new TerritorialCatalogDetailDto(dto, ancestors,
            unit.Names.OrderByDescending(x => x.IsPrimary).Select(x => new TerritorialCatalogNameDto(x.Id, x.Name, x.Locale, x.Kind, x.IsPrimary, x.DatasetSource == null ? null : x.DatasetSource.Organisation + " · " + x.DatasetSource.Dataset)).ToArray(),
            unit.Codes.OrderByDescending(x => x.IsPrimary).Select(x => new TerritorialCatalogCodeDto(x.Id, x.Scheme, x.Value, x.ValidFrom, x.ValidTo, x.IsPrimary, x.DatasetSource == null ? null : x.DatasetSource.Organisation + " · " + x.DatasetSource.Dataset)).ToArray(),
            unit.CoordinateSource == null ? null : unit.CoordinateSource.Organisation + " · " + unit.CoordinateSource.Dataset,
            unit.Names.Select(x => x.DatasetSource?.Dataset).Concat(unit.Codes.Select(x => x.DatasetSource?.Dataset)).Where(x => x != null).Select(x => x!).Distinct().ToArray(),
            imports,
            unit.MaintenanceAudit.OrderByDescending(x => x.CreatedAtUtc).Select(x => new TerritorialMaintenanceAuditDto(
                x.Id, x.Action, x.Field, x.BeforeValue, x.AfterValue, x.Reason, x.ActorUserId,
                x.ActorUser.DisplayName ?? x.ActorUser.Email, x.Origin, x.CreatedAtUtc)).ToArray());
    }

    public async Task<TerritorialCatalogDetailDto> MaintainAsync(Guid id, Guid actorUserId, TerritorialMaintenanceRequest request, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var unit = await db.TerritorialUnits.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("No s'ha trobat la unitat territorial.");
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length < 3 || reason.Length > 500) throw new InvalidOperationException("El motiu és obligatori i ha de tenir entre 3 i 500 caràcters.");
        string field; string before; string after;
        switch (request.Action.Trim().ToLowerInvariant())
        {
            case "activate":
                field = "isActive"; before = JsonSerializer.Serialize(unit.IsActive); unit.IsActive = true; unit.HasManualActiveOverride = true; after = "true"; break;
            case "deactivate":
                field = "isActive"; before = JsonSerializer.Serialize(unit.IsActive); unit.IsActive = false; unit.HasManualActiveOverride = true; after = "false"; break;
            case "set-selectable":
                if (request.IsSelectableLocality is null) throw new InvalidOperationException("Cal indicar si la unitat és seleccionable.");
                field = "isSelectableLocality"; before = JsonSerializer.Serialize(unit.ManualSelectableLocality); unit.ManualSelectableLocality = request.IsSelectableLocality; after = JsonSerializer.Serialize(request.IsSelectableLocality); break;
            case "set-coordinates":
                if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180 ||
                    request.Latitude is null || request.Longitude is null || (request.Latitude == 0 && request.Longitude == 0))
                    throw new InvalidOperationException("Les coordenades han de ser completes, vàlides i diferents de (0,0).");
                field = "coordinates"; before = JsonSerializer.Serialize(new { unit.Latitude, unit.Longitude });
                unit.Latitude = request.Latitude; unit.Longitude = request.Longitude; unit.CoordinateSourceId = null; unit.HasManualCoordinateOverride = true;
                after = JsonSerializer.Serialize(new { request.Latitude, request.Longitude }); break;
            default: throw new InvalidOperationException("L'operació de manteniment no és compatible.");
        }
        unit.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.TerritorialMaintenanceAudit.Add(new TerritorialMaintenanceAuditRecord
        {
            Id = Guid.NewGuid(), TerritorialUnitId = id, Action = request.Action.Trim(), Field = field,
            BeforeValue = before, AfterValue = after, Reason = reason, ActorUserId = actorUserId,
            Origin = "Manual", CreatedAtUtc = unit.UpdatedAtUtc
        });
        var updatedCatalog = await db.TerritorialCatalogStates.Where(x => x.CountryId == unit.CountryId)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.Version, x => x.Version + 1)
                .SetProperty(x => x.UpdatedAtUtc, unit.UpdatedAtUtc), ct);
        if (updatedCatalog != 1) throw new DbUpdateConcurrencyException("No s'ha pogut versionar el manteniment territorial.");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return await GetCatalogUnitAsync(id, ct) ?? throw new InvalidOperationException("No s'ha pogut recuperar la unitat territorial.");
    }

    private static TerritorialMappingTemplateDetailDto MappingDetail(TerritorialMappingTemplateRecord record) =>
        new(record.Id, record.DatasetSourceId, record.Version, record.SchemaFingerprint, record.DefinitionChecksum,
            record.IsActive, record.CreatedAtUtc, JsonDocument.Parse(record.DefinitionJson).RootElement.Clone());

    private static JsonElement? ParseOptional(string? json) => json is null ? null : JsonDocument.Parse(json).RootElement.Clone();

    private static TerritorialCatalogUnitDto ToCatalogUnit(TerritorialUnitRecord x) => new(
        x.Id, x.CountryId, x.Country.Name, x.TerritorialUnitTypeId, x.TerritorialUnitType.Code, x.TerritorialUnitType.Name,
        x.ParentId, x.Parent?.Names.FirstOrDefault(n => n.IsPrimary)?.Name,
        x.Codes.FirstOrDefault(c => c.IsPrimary)?.Value ?? x.Codes.FirstOrDefault()?.Value,
        x.Names.FirstOrDefault(n => n.IsPrimary)?.Name ?? x.Names.FirstOrDefault()?.Name ?? string.Empty,
        x.Names.FirstOrDefault(n => n.IsPrimary)?.Locale, x.Latitude, x.Longitude, x.IsActive,
        x.ManualSelectableLocality ?? x.TerritorialUnitType.IsSelectableLocality,
        x.HasManualActiveOverride || x.ManualSelectableLocality != null || x.HasManualCoordinateOverride,
        x.HasManualActiveOverride, x.ManualSelectableLocality != null, x.HasManualCoordinateOverride);
}
