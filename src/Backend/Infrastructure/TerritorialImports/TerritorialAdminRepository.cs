using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
            .Select(x => new TerritorialAdminSourceDto(x.Id, x.CountryId, x.Organisation, x.Dataset,
                x.DatasetType, x.Locale, x.ApprovalStatus, x.IsActive, x.PublicationMode, x.DatasetVersion,
                x.DatasetDate, x.License, x.Attribution)).ToArrayAsync(ct);
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
                x.CurrentStage, x.TotalRows, x.ProcessedRows, x.ProcessingStartedAtUtc, x.ProcessingCompletedAtUtc,
                x.LastHeartbeatAtUtc, x.AttemptCount, x.LastErrorCode, x.LastErrorMessage, x.IsRecoverable,
                x.CancellationRequested,
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
                    x.CurrentStage, x.TotalRows, x.ProcessedRows, x.ProcessingStartedAtUtc, x.ProcessingCompletedAtUtc,
                    x.LastHeartbeatAtUtc, x.AttemptCount, x.LastErrorCode, x.LastErrorMessage, x.IsRecoverable,
                    x.CancellationRequested,
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
                SourceApprovalStatus = x.DatasetSource.ApprovalStatus,
                SourceIsActive = x.DatasetSource.IsActive,
                MappingIsActive = x.MappingTemplate.IsActive,
                CurrentCatalogVersion = db.TerritorialCatalogStates.Where(state => state.CountryId == x.DatasetSource.CountryId).Select(state => (long?)state.Version).FirstOrDefault() ?? 0,
                ChangeSet = x.ChangeSets.Where(set => set.RevertsChangeSetId == null).Select(set => new { set.Id, set.Status, set.CatalogVersion, set.CreatedAtUtc, set.PublishedAtUtc }).FirstOrDefault()
            }).SingleOrDefaultAsync(ct);
        if (item is null) return null;
        var changeSet = item.ChangeSet;
        var canRevert = item.Summary.Status == "Published" && changeSet is not null && item.CurrentCatalogVersion == changeSet.CatalogVersion + 1;
        var sourceSheets = await db.TerritorialImportRows.AsNoTracking().Where(x => x.ImportId == id)
            .Select(x => x.Sheet).Distinct().OrderBy(x => x).ToArrayAsync(ct);
        var publicationItems = await db.TerritorialChangeSetItems.AsNoTracking()
            .Where(x => x.ChangeSet.ImportId == id && x.ChangeSet.RevertsChangeSetId == null)
            .Select(x => new { x.Kind, x.BeforeJson, x.AfterJson, x.ChangedFieldsJson }).ToArrayAsync(ct);
        var typeNames = await db.TerritorialUnitTypes.AsNoTracking().Where(x => x.CountryId == item.Summary.CountryId)
            .ToDictionaryAsync(x => x.Code, x => x.Name, StringComparer.OrdinalIgnoreCase, ct);
        var breakdown = publicationItems.Select(x => new
            {
                x.Kind,
                TypeCode = x.AfterJson != null
                    ? JsonSerializer.Deserialize<CanonicalTerritorialUnit>(x.AfterJson, TerritorialImportJson.Options)?.TerritorialUnitTypeCode
                    : x.BeforeJson != null
                        ? JsonSerializer.Deserialize<TerritorialCatalogUnitSnapshot>(x.BeforeJson, TerritorialImportJson.Options)?.TerritorialUnitTypeCode
                        : null
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.TypeCode))
            .GroupBy(x => x.TypeCode!, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TerritorialPublicationBreakdownDto(group.Key, typeNames.GetValueOrDefault(group.Key) ?? group.Key,
                group.Count(x => x.Kind == "Create"), group.Count(x => x.Kind == "Update"),
                group.Count(x => x.Kind == "Deactivate"), group.Count(x => x.Kind == "NoChange")))
            .OrderBy(x => x.TerritorialUnitType).ToArray();
        var conflictCount = publicationItems.Count(x =>
            (JsonSerializer.Deserialize<string[]>(x.ChangedFieldsJson, TerritorialImportJson.Options) ?? [])
                .Any(field => field.StartsWith("manualOverrideConflict:", StringComparison.Ordinal)));
        var canPublish = item.Summary.Status == "ReadyForReview"
            && !item.Summary.HasBlockingErrors
            && item.SourceIsActive
            && item.SourceApprovalStatus == "Approved"
            && item.MappingIsActive
            && changeSet?.Status == "Prepared"
            && item.Summary.CatalogVersion == item.CurrentCatalogVersion
            && conflictCount == 0;
        return new TerritorialImportDetailDto(item.Summary, item.PublicationMode, item.FailureReason, item.SchemaFingerprint,
            item.Summary.Status is not ("Published" or "Reverted" or "Cancelled"),
            canPublish,
            canRevert, item.CurrentCatalogVersion, changeSet?.Id, changeSet?.Status, changeSet?.CreatedAtUtc, changeSet?.PublishedAtUtc,
            sourceSheets, conflictCount, breakdown);
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
        IQueryable<TerritorialChangeSetItemRecord> query;
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{EscapeLikePattern(request.Search.Trim())}%";
            query = db.TerritorialChangeSetItems.FromSqlInterpolated($$"""
                SELECT *
                FROM territorial_change_set_items
                WHERE canonical_unit_key ILIKE {{pattern}} ESCAPE '\'
                   OR before_json::text ILIKE {{pattern}} ESCAPE '\'
                   OR after_json::text ILIKE {{pattern}} ESCAPE '\'
                """).AsNoTracking();
        }
        else query = db.TerritorialChangeSetItems.AsNoTracking();
        query = query.Where(x => x.ChangeSet.ImportId == importId && x.ChangeSet.RevertsChangeSetId == null);
        if (!string.IsNullOrWhiteSpace(request.Kind)) query = query.Where(x => x.Kind == request.Kind.Trim());
        var total = await query.CountAsync(ct);
        var raw = await query.OrderBy(x => x.Kind == "Deactivate" ? 0 : x.Kind == "Update" ? 1 : x.Kind == "Create" ? 2 : 3)
            .ThenBy(x => x.CanonicalUnitKey).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new RawChange(x.Id, x.Kind, x.TerritorialUnitId, x.CanonicalUnitKey, x.BeforeJson, x.AfterJson, x.ChangedFieldsJson))
            .ToArrayAsync(ct);

        var rows = await BuildChangeDtos(importId, raw, ct);
        return new PageResult<TerritorialChangeItemDto>(rows, request.Page, request.PageSize, total);
    }

    public async Task<TerritorialChangeHierarchyDto?> GetChangeHierarchyAsync(
        Guid importId, Guid changeId, TerritorialChangeHierarchyQuery request, CancellationToken ct = default)
    {
        var selected = await db.TerritorialChangeSetItems.AsNoTracking()
            .Where(x => x.Id == changeId && x.ChangeSet.ImportId == importId && x.ChangeSet.RevertsChangeSetId == null)
            .Select(x => new
            {
                x.ChangeSetId,
                Change = new RawChange(x.Id, x.Kind, x.TerritorialUnitId, x.CanonicalUnitKey, x.BeforeJson, x.AfterJson, x.ChangedFieldsJson)
            }).SingleOrDefaultAsync(ct);
        if (selected is null) return null;

        var ancestors = new List<RawChange>();
        var currentMaterial = ToMaterial(selected.Change);
        var visited = new HashSet<Guid> { currentMaterial.Id };
        while (ancestors.Count < 32)
        {
            RawChange? parent = null;
            if (currentMaterial.After?.ParentCanonicalUnitKey is { } parentKey)
            {
                parent = await db.TerritorialChangeSetItems.AsNoTracking()
                    .Where(x => x.ChangeSetId == selected.ChangeSetId && x.CanonicalUnitKey == parentKey)
                    .Select(x => new RawChange(x.Id, x.Kind, x.TerritorialUnitId, x.CanonicalUnitKey, x.BeforeJson, x.AfterJson, x.ChangedFieldsJson))
                    .FirstOrDefaultAsync(ct);
            }
            else if (currentMaterial.Before?.ParentId is { } parentId)
            {
                parent = await db.TerritorialChangeSetItems.AsNoTracking()
                    .Where(x => x.ChangeSetId == selected.ChangeSetId && x.TerritorialUnitId == parentId)
                    .Select(x => new RawChange(x.Id, x.Kind, x.TerritorialUnitId, x.CanonicalUnitKey, x.BeforeJson, x.AfterJson, x.ChangedFieldsJson))
                    .FirstOrDefaultAsync(ct);
            }
            if (parent is null || !visited.Add(parent.Id)) break;
            ancestors.Insert(0, parent);
            currentMaterial = ToMaterial(parent);
        }

        var childrenQuery = DirectChildrenQuery(selected.ChangeSetId, selected.Change.CanonicalUnitKey,
            selected.Change.TerritorialUnitId, request.Search);
        var total = await childrenQuery.CountAsync(ct);
        var childRows = await childrenQuery.OrderBy(x => x.CanonicalUnitKey)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new RawChange(x.Id, x.Kind, x.TerritorialUnitId, x.CanonicalUnitKey, x.BeforeJson, x.AfterJson, x.ChangedFieldsJson))
            .ToArrayAsync(ct);
        var allRows = ancestors.Append(selected.Change).Concat(childRows).DistinctBy(x => x.Id).ToArray();
        var presented = (await BuildChangeDtos(importId, allRows, ct)).ToDictionary(x => x.Id);
        var children = childRows.Select(x => ToHierarchyNode(presented[x.Id])).ToArray();
        return new TerritorialChangeHierarchyDto(
            presented[selected.Change.Id], ancestors.Select(x => ToHierarchyNode(presented[x.Id])).ToArray(),
            new PageResult<TerritorialChangeHierarchyNodeDto>(children, request.Page, request.PageSize, total));
    }

    private IQueryable<TerritorialChangeSetItemRecord> DirectChildrenQuery(
        Guid changeSetId, string canonicalUnitKey, Guid? territorialUnitId, string? search)
    {
        var pattern = string.IsNullOrWhiteSpace(search) ? null : $"%{EscapeLikePattern(search.Trim())}%";
        var parameters = new List<object>
        {
            new NpgsqlParameter("change_set_id", changeSetId),
            new NpgsqlParameter("canonical_unit_key", canonicalUnitKey)
        };
        string sql;
        if (territorialUnitId is { } parentId)
        {
            parameters.Add(new NpgsqlParameter("parent_id", parentId.ToString()));
            sql = """
                SELECT * FROM territorial_change_set_items
                WHERE change_set_id = @change_set_id
                  AND ((after_json ->> 'parentCanonicalUnitKey') = @canonical_unit_key
                       OR (after_json IS NULL AND (before_json ->> 'parentId') = @parent_id))
                """;
            if (pattern is not null)
            {
                parameters.Add(new NpgsqlParameter("pattern", pattern));
                sql += """

                      AND (canonical_unit_key ILIKE @pattern ESCAPE '\'
                           OR before_json::text ILIKE @pattern ESCAPE '\'
                           OR after_json::text ILIKE @pattern ESCAPE '\')
                    """;
            }
        }
        else
        {
            sql = """
                SELECT * FROM territorial_change_set_items
                WHERE change_set_id = @change_set_id
                  AND (after_json ->> 'parentCanonicalUnitKey') = @canonical_unit_key
                """;
            if (pattern is not null)
            {
                parameters.Add(new NpgsqlParameter("pattern", pattern));
                sql += """

                      AND (canonical_unit_key ILIKE @pattern ESCAPE '\'
                           OR after_json::text ILIKE @pattern ESCAPE '\')
                    """;
            }
        }
        return db.TerritorialChangeSetItems.FromSqlRaw(sql, parameters.ToArray()).AsNoTracking();
    }

    private async Task<IReadOnlyCollection<TerritorialChangeItemDto>> BuildChangeDtos(
        Guid importId, IReadOnlyCollection<RawChange> raw, CancellationToken ct)
    {

        var importContext = await db.TerritorialImports.AsNoTracking().Where(x => x.Id == importId)
            .Select(x => new
            {
                x.DatasetSource.CountryId,
                Country = x.DatasetSource.Country.Name,
                Source = x.DatasetSource.Organisation + " · " + x.DatasetSource.Dataset,
                x.DatasetSource.Organisation,
                x.DatasetSource.Dataset,
                x.DatasetVersion,
                x.DatasetSource.DatasetDate,
                MappingVersion = x.MappingTemplate.Version,
                x.DatasetSource.Locale,
                SourceUrl = x.DatasetSource.Url
            }).SingleAsync(ct);
        var types = await db.TerritorialUnitTypes.AsNoTracking().Where(x => x.CountryId == importContext.CountryId)
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, ct);

        var materials = raw.Select(ToMaterial).ToArray();
        var canonicalAncestors = await LoadCanonicalAncestors(importId,
            materials.Select(x => x.After?.ParentCanonicalUnitKey).Where(x => x is not null).Select(x => x!).Distinct(), ct);
        var canonicalParents = canonicalAncestors.ToDictionary(x => x.Key, x => x.Value.Name, StringComparer.Ordinal);
        var existingAncestors = await LoadExistingAncestors(
            materials.Select(x => x.Before?.ParentId).Where(x => x is not null).Select(x => x!.Value).Distinct(), ct);
        var existingParents = existingAncestors.ToDictionary(x => x.Key, x => x.Value.Name);

        return materials.Select(item =>
        {
            var names = item.After?.Names ?? item.Before?.Names ?? [];
            var codes = item.After?.Codes ?? item.Before?.Codes ?? [];
            var typeCode = item.After?.TerritorialUnitTypeCode ?? item.Before?.TerritorialUnitTypeCode ?? string.Empty;
            var type = types.GetValueOrDefault(typeCode);
            var parent = item.After?.ParentCanonicalUnitKey is { } parentKey
                ? canonicalParents.GetValueOrDefault(parentKey)
                : item.Before?.ParentId is { } parentId ? existingParents.GetValueOrDefault(parentId) : null;
            var isActive = item.Kind == "Deactivate" ? false : item.After?.IsActive ?? item.Before?.IsActive ?? false;
            var primaryName = names.FirstOrDefault(x => x.IsPrimary) ?? names.FirstOrDefault();
            var primaryCode = codes.FirstOrDefault(x => x.IsPrimary) ?? codes.FirstOrDefault();
            return new TerritorialChangeItemDto(
                item.Id, item.Kind, item.TerritorialUnitId, primaryName?.Name ?? "Sense nom", primaryCode?.Value,
                typeCode, type?.Name ?? typeCode, importContext.Country, parent, primaryName?.Locale, isActive,
                type?.IsSelectableLocality ?? false, item.After?.Latitude ?? item.Before?.Latitude,
                item.After?.Longitude ?? item.Before?.Longitude, importContext.Source,
                BuildHierarchy(importContext.Country, primaryName?.Name ?? "Sense nom", item, canonicalAncestors, existingAncestors),
                new TerritorialChangeProvenanceDto(importContext.Organisation, importContext.Dataset,
                    importContext.DatasetVersion, importContext.DatasetDate, importContext.MappingVersion,
                    importContext.Locale, string.IsNullOrWhiteSpace(importContext.SourceUrl) ? null : importContext.SourceUrl),
                names.Select(x => new TerritorialChangeNameDto(x.Name, x.Locale, x.Kind, x.IsPrimary, importContext.Source)).ToArray(),
                codes.Select(x => new TerritorialChangeCodeDto(x.Scheme, x.Value, x.IsPrimary, x.ValidFrom, x.ValidTo, importContext.Source)).ToArray(),
                Differences(item, canonicalParents, existingParents),
                item.Kind == "Create" ? "Aquesta unitat territorial encara no existeix i es crearà en publicar."
                    : item.Kind == "Deactivate" ? "La unitat ja no apareix al dataset oficial actual."
                    : null,
                item.ChangedFields.Any(x => x.StartsWith("manualOverrideConflict:", StringComparison.Ordinal)));
        }).ToArray();
    }

    private static ChangeMaterial ToMaterial(RawChange item) => new(
        item.Id, item.Kind, item.TerritorialUnitId,
        item.BeforeJson is null ? null : JsonSerializer.Deserialize<TerritorialCatalogUnitSnapshot>(item.BeforeJson, TerritorialImportJson.Options),
        item.AfterJson is null ? null : JsonSerializer.Deserialize<CanonicalTerritorialUnit>(item.AfterJson, TerritorialImportJson.Options),
        JsonSerializer.Deserialize<string[]>(item.ChangedFieldsJson, TerritorialImportJson.Options) ?? []);

    private static TerritorialChangeHierarchyNodeDto ToHierarchyNode(TerritorialChangeItemDto item) => new(
        item.Id, item.Name, item.PrimaryCode, item.TerritorialUnitType, item.Kind, item.HasManualConflict);

    private async Task<Dictionary<string, CanonicalAncestor>> LoadCanonicalAncestors(
        Guid importId, IEnumerable<string> initialKeys, CancellationToken ct)
    {
        var result = new Dictionary<string, CanonicalAncestor>(StringComparer.Ordinal);
        var pending = initialKeys.Distinct(StringComparer.Ordinal).ToArray();
        var attempted = new HashSet<string>(StringComparer.Ordinal);
        while (pending.Length > 0)
        {
            foreach (var key in pending) attempted.Add(key);
            var raw = await db.TerritorialImportRows.AsNoTracking()
                .Where(x => x.ImportId == importId && pending.Contains(x.CanonicalUnitKey))
                .Select(x => new { x.CanonicalUnitKey, x.CanonicalJson }).ToArrayAsync(ct);
            foreach (var group in raw.GroupBy(x => x.CanonicalUnitKey, StringComparer.Ordinal))
            {
                var unit = JsonSerializer.Deserialize<CanonicalTerritorialUnit>(group.First().CanonicalJson, TerritorialImportJson.Options)!;
                result[group.Key] = new CanonicalAncestor(PrimaryName(unit.Names), unit.ParentCanonicalUnitKey);
            }
            pending = result.Values.Select(x => x.ParentKey).Where(x => x is not null)
                .Select(x => x!).Where(x => !attempted.Contains(x)).Distinct(StringComparer.Ordinal).ToArray();
        }
        return result;
    }

    private async Task<Dictionary<Guid, ExistingAncestor>> LoadExistingAncestors(
        IEnumerable<Guid> initialIds, CancellationToken ct)
    {
        var result = new Dictionary<Guid, ExistingAncestor>();
        var pending = initialIds.Distinct().ToArray();
        var attempted = new HashSet<Guid>();
        while (pending.Length > 0)
        {
            foreach (var id in pending) attempted.Add(id);
            var rows = await db.TerritorialUnits.AsNoTracking().Where(x => pending.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.ParentId,
                    Name = x.Names.Where(n => n.IsPrimary).Select(n => n.Name).FirstOrDefault()
                        ?? x.Names.Select(n => n.Name).FirstOrDefault() ?? string.Empty
                }).ToArrayAsync(ct);
            foreach (var row in rows) result[row.Id] = new ExistingAncestor(row.Name, row.ParentId);
            pending = result.Values.Select(x => x.ParentId).Where(x => x is not null).Select(x => x!.Value)
                .Where(x => !attempted.Contains(x)).Distinct().ToArray();
        }
        return result;
    }

    private static IReadOnlyCollection<string> BuildHierarchy(
        string country, string name, ChangeMaterial item,
        IReadOnlyDictionary<string, CanonicalAncestor> canonicalAncestors,
        IReadOnlyDictionary<Guid, ExistingAncestor> existingAncestors)
    {
        var hierarchy = new List<string> { country };
        var ancestors = new List<string>();
        if (item.After?.ParentCanonicalUnitKey is { } canonicalKey)
        {
            while (canonicalAncestors.TryGetValue(canonicalKey, out var ancestor))
            {
                ancestors.Insert(0, ancestor.Name);
                if (ancestor.ParentKey is null) break;
                canonicalKey = ancestor.ParentKey;
            }
        }
        else if (item.Before?.ParentId is { } existingId)
        {
            while (existingAncestors.TryGetValue(existingId, out var ancestor))
            {
                ancestors.Insert(0, ancestor.Name);
                if (ancestor.ParentId is null) break;
                existingId = ancestor.ParentId.Value;
            }
        }
        hierarchy.AddRange(ancestors.Where(x => !string.Equals(x, country, StringComparison.OrdinalIgnoreCase)));
        if (!string.Equals(name, country, StringComparison.OrdinalIgnoreCase)) hierarchy.Add(name);
        return hierarchy;
    }

    public async Task<PageResult<TerritorialSourcePreviewRowDto>> ListSourcePreviewAsync(Guid importId, TerritorialPreviewQuery request, CancellationToken ct = default)
    {
        IQueryable<TerritorialImportRowRecord> query;
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{EscapeLikePattern(request.Search.Trim())}%";
            query = db.TerritorialImportRows.FromSqlInterpolated($$"""
                SELECT * FROM territorial_import_rows WHERE source_json::text ILIKE {{pattern}} ESCAPE '\'
                """).AsNoTracking();
        }
        else query = db.TerritorialImportRows.AsNoTracking();
        query = query.Where(x => x.ImportId == importId);
        if (!string.IsNullOrWhiteSpace(request.Sheet)) query = query.Where(x => x.Sheet == request.Sheet.Trim());
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
        IQueryable<TerritorialImportRowRecord> query;
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{EscapeLikePattern(request.Search.Trim())}%";
            query = db.TerritorialImportRows.FromSqlInterpolated($$"""
                SELECT *
                FROM territorial_import_rows
                WHERE canonical_unit_key ILIKE {{pattern}} ESCAPE '\'
                   OR canonical_json::text ILIKE {{pattern}} ESCAPE '\'
                """).AsNoTracking();
        }
        else query = db.TerritorialImportRows.AsNoTracking();
        query = query.Where(x => x.ImportId == importId);
        if (!string.IsNullOrWhiteSpace(request.Sheet)) query = query.Where(x => x.Sheet == request.Sheet.Trim());
        var total = await query.CountAsync(ct);
        var raw = await query.OrderBy(x => x.Sheet).ThenBy(x => x.RowNumber)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new { x.Id, x.Sheet, x.RowNumber, x.CanonicalJson }).ToArrayAsync(ct);
        var sheets = raw.Select(x => x.Sheet).Distinct().ToArray();
        var rowNumbers = raw.Select(x => x.RowNumber).Distinct().ToArray();
        var relatedIssues = raw.Length == 0 ? [] : await db.TerritorialImportIssues.AsNoTracking()
            .Where(x => x.ImportId == importId && x.Sheet != null && x.RowNumber != null &&
                sheets.Contains(x.Sheet) && rowNumbers.Contains(x.RowNumber.Value))
            .OrderBy(x => x.Severity == "Error" ? 0 : 1).ThenBy(x => x.RuleCode)
            .Select(x => new TerritorialCanonicalPreviewIssueDto(
                x.Severity, x.RuleCode, x.Sheet!, x.RowNumber!.Value, x.Field, x.Message))
            .ToArrayAsync(ct);
        var rows = raw.Select(x =>
        {
            var unit = JsonSerializer.Deserialize<CanonicalTerritorialUnit>(x.CanonicalJson, TerritorialImportJson.Options)!;
            var name = unit.Names.FirstOrDefault(item => item.IsPrimary) ?? unit.Names.FirstOrDefault();
            var issues = relatedIssues.Where(issue => issue.Sheet == x.Sheet && issue.RowNumber == x.RowNumber).ToArray();
            return new TerritorialCanonicalPreviewRowDto(x.Id, x.Sheet, x.RowNumber, unit.CanonicalUnitKey,
                unit.ParentCanonicalUnitKey, name?.Name ?? string.Empty, name?.Locale, unit.TerritorialUnitTypeCode,
                unit.Codes, unit.Latitude, unit.Longitude,
                issues.Any(issue => issue.Severity == "Error") ? "Invàlida" : "Vàlida", issues.Length, issues);
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

    public async Task<TerritorialCatalogHierarchyDto?> GetCatalogHierarchyAsync(
        Guid id, TerritorialCatalogHierarchyQuery request, CancellationToken ct = default)
    {
        var path = await db.Database.SqlQueryRaw<CatalogPathRow>("""
            WITH RECURSIVE path AS (
                SELECT id, parent_id, 0 AS depth
                FROM territorial_units
                WHERE id = @unit_id
                UNION ALL
                SELECT parent.id, parent.parent_id, path.depth + 1
                FROM territorial_units parent
                JOIN path ON path.parent_id = parent.id
            )
            SELECT id AS "Id", depth AS "Depth" FROM path
            """, new NpgsqlParameter("unit_id", id)).ToArrayAsync(ct);
        if (path.Length == 0) return null;

        var pathIds = path.Select(x => x.Id).ToArray();
        var pathNodes = await CatalogHierarchyNodes(db.TerritorialUnits.AsNoTracking().Where(x => pathIds.Contains(x.Id)))
            .ToDictionaryAsync(x => x.Id, ct);
        var current = pathNodes[id];
        var ancestors = path.Where(x => x.Depth > 0).OrderByDescending(x => x.Depth)
            .Select(x => pathNodes[x.Id]).ToArray();

        var childrenQuery = db.TerritorialUnits.AsNoTracking().Where(x => x.ParentId == id);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var value = request.Search.Trim();
            childrenQuery = childrenQuery.Where(x => x.Names.Any(n => n.Name.Contains(value)) || x.Codes.Any(c => c.Value.Contains(value)));
        }
        var total = await childrenQuery.CountAsync(ct);
        var childrenPage = childrenQuery
            .OrderBy(x => x.TerritorialUnitType.DisplayOrder)
            .ThenBy(x => x.Names.Where(n => n.IsPrimary).Select(n => n.Name).FirstOrDefault())
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize);
        var children = await CatalogHierarchyNodes(childrenPage).ToArrayAsync(ct);

        var descendantTypes = await db.Database.SqlQueryRaw<CatalogDescendantTypeRow>("""
            WITH RECURSIVE subtree AS (
                SELECT id, parent_id, territorial_unit_type_id, 0 AS depth
                FROM territorial_units
                WHERE id = @unit_id
                UNION ALL
                SELECT child.id, child.parent_id, child.territorial_unit_type_id, subtree.depth + 1
                FROM territorial_units child
                JOIN subtree ON child.parent_id = subtree.id
            )
            SELECT type.id AS "TerritorialUnitTypeId", type.code AS "TypeCode", type.name AS "Type",
                   count(*)::int AS "Count", type.is_selectable_locality AS "IsSelectableLocality"
            FROM subtree
            JOIN territorial_unit_types type ON type.id = subtree.territorial_unit_type_id
            WHERE subtree.depth > 0
            GROUP BY type.id, type.code, type.name, type.display_order, type.is_selectable_locality
            ORDER BY type.display_order, type.name
            """, new NpgsqlParameter("unit_id", id)).ToArrayAsync(ct);

        return new TerritorialCatalogHierarchyDto(current, ancestors,
            new PageResult<TerritorialCatalogHierarchyNodeDto>(children, request.Page, request.PageSize, total),
            descendantTypes.Select(x => new TerritorialCatalogDescendantTypeDto(
                x.TerritorialUnitTypeId, x.TypeCode, x.Type, x.Count, x.IsSelectableLocality)).ToArray());
    }

    public async Task<PageResult<TerritorialCatalogUnitDto>> ListCatalogDescendantsAsync(
        Guid id, TerritorialCatalogDescendantQuery request, CancellationToken ct = default)
    {
        var pattern = $"%{EscapeLikePattern(request.Search?.Trim() ?? string.Empty)}%";
        var offset = (request.Page - 1) * request.PageSize;
        var rows = await db.Database.SqlQueryRaw<CatalogDescendantRow>("""
            WITH RECURSIVE subtree AS (
                SELECT id, parent_id, country_id, territorial_unit_type_id, 0 AS depth
                FROM territorial_units
                WHERE id = @unit_id
                UNION ALL
                SELECT child.id, child.parent_id, child.country_id, child.territorial_unit_type_id, subtree.depth + 1
                FROM territorial_units child
                JOIN subtree ON child.parent_id = subtree.id
            ), matches AS (
                SELECT unit.id AS "Id", unit.country_id AS "CountryId", country.name AS "Country",
                       unit.territorial_unit_type_id AS "TerritorialUnitTypeId", type.code AS "TypeCode", type.name AS "Type",
                       unit.parent_id AS "ParentId", parent_name.name AS "Parent", code.value AS "PrimaryCode",
                       name.name AS "PrimaryName", name.locale AS "Locale", unit.latitude AS "Latitude",
                       unit.longitude AS "Longitude", unit.is_active AS "IsActive",
                       COALESCE(unit.manual_selectable_locality, type.is_selectable_locality) AS "IsSelectableLocality",
                       (unit.has_manual_active_override OR unit.manual_selectable_locality IS NOT NULL OR unit.has_manual_coordinate_override) AS "HasManualOverride",
                       unit.has_manual_active_override AS "HasManualActiveOverride",
                       (unit.manual_selectable_locality IS NOT NULL) AS "HasManualSelectableOverride",
                       unit.has_manual_coordinate_override AS "HasManualCoordinateOverride"
                FROM subtree
                JOIN territorial_units unit ON unit.id = subtree.id
                JOIN countries country ON country.id = unit.country_id
                JOIN territorial_unit_types type ON type.id = unit.territorial_unit_type_id
                LEFT JOIN LATERAL (
                    SELECT n.name, n.locale FROM territorial_unit_names n
                    WHERE n.territorial_unit_id = unit.id ORDER BY n.is_primary DESC, n.id LIMIT 1
                ) name ON TRUE
                LEFT JOIN LATERAL (
                    SELECT c.value FROM territorial_unit_codes c
                    WHERE c.territorial_unit_id = unit.id ORDER BY c.is_primary DESC, c.id LIMIT 1
                ) code ON TRUE
                LEFT JOIN LATERAL (
                    SELECT n.name FROM territorial_unit_names n
                    WHERE n.territorial_unit_id = unit.parent_id ORDER BY n.is_primary DESC, n.id LIMIT 1
                ) parent_name ON TRUE
                WHERE subtree.depth > 0
                  AND unit.territorial_unit_type_id = @type_id
                  AND (COALESCE(name.name, '') ILIKE @pattern ESCAPE '\'
                       OR COALESCE(code.value, '') ILIKE @pattern ESCAPE '\')
            )
            SELECT matches.*, count(*) OVER()::int AS "TotalCount"
            FROM matches
            ORDER BY "PrimaryName", "PrimaryCode"
            LIMIT @page_size OFFSET @offset
            """,
            new NpgsqlParameter("unit_id", id),
            new NpgsqlParameter("type_id", request.TerritorialUnitTypeId),
            new NpgsqlParameter("pattern", pattern),
            new NpgsqlParameter("page_size", request.PageSize),
            new NpgsqlParameter("offset", offset)).ToArrayAsync(ct);
        var total = rows.FirstOrDefault()?.TotalCount ?? 0;
        return new PageResult<TerritorialCatalogUnitDto>(rows.Select(ToCatalogUnit).ToArray(), request.Page, request.PageSize, total);
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

    private static IReadOnlyCollection<TerritorialFieldDifferenceDto> Differences(
        ChangeMaterial item,
        IReadOnlyDictionary<string, string> canonicalParents,
        IReadOnlyDictionary<Guid, string> existingParents)
    {
        if (item.Kind == "Create") return [];
        if (item.Kind == "Deactivate") return [new("isActive", "Sí", "No", false)];
        var result = new List<TerritorialFieldDifferenceDto>();
        foreach (var rawField in item.ChangedFields)
        {
            var conflict = rawField.StartsWith("manualOverrideConflict:", StringComparison.Ordinal);
            var field = conflict ? rawField["manualOverrideConflict:".Length..] : rawField;
            if (result.Any(x => x.Field == field)) continue;
            result.Add(new TerritorialFieldDifferenceDto(field,
                FieldValue(field, item.Before, null, existingParents),
                FieldValue(field, null, item.After, canonicalParents: canonicalParents), conflict));
        }
        return result;
    }

    private static string? FieldValue(
        string field,
        TerritorialCatalogUnitSnapshot? before,
        CanonicalTerritorialUnit? after,
        IReadOnlyDictionary<Guid, string>? existingParents = null,
        IReadOnlyDictionary<string, string>? canonicalParents = null) => field switch
    {
        "names" => string.Join(" · ", (after?.Names ?? before?.Names ?? []).Where(x => x.IsPrimary).Select(x => x.Name)),
        "codes" => string.Join(" · ", (after?.Codes ?? before?.Codes ?? []).Where(x => x.IsPrimary).Select(x => x.Value)),
        "territorialUnitType" => after?.TerritorialUnitTypeCode ?? before?.TerritorialUnitTypeCode,
        "isActive" => (after?.IsActive ?? before?.IsActive) is true ? "Sí" : "No",
        "coordinates" => FormatCoordinates(after?.Latitude ?? before?.Latitude, after?.Longitude ?? before?.Longitude),
        "parent" => after?.ParentCanonicalUnitKey is { } key ? canonicalParents?.GetValueOrDefault(key)
            : before?.ParentId is { } id ? existingParents?.GetValueOrDefault(id) : "—",
        _ => null
    };

    private static string FormatCoordinates(decimal? latitude, decimal? longitude) =>
        latitude is null || longitude is null ? "—" : $"{latitude:0.######}, {longitude:0.######}";

    private static string PrimaryName(IReadOnlyCollection<CanonicalTerritorialName> names) =>
        (names.FirstOrDefault(x => x.IsPrimary) ?? names.FirstOrDefault())?.Name ?? "Sense nom";

    private static string EscapeLikePattern(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);

    private sealed record ChangeMaterial(
        Guid Id,
        string Kind,
        Guid? TerritorialUnitId,
        TerritorialCatalogUnitSnapshot? Before,
        CanonicalTerritorialUnit? After,
        IReadOnlyCollection<string> ChangedFields);

    private sealed record RawChange(
        Guid Id,
        string Kind,
        Guid? TerritorialUnitId,
        string CanonicalUnitKey,
        string? BeforeJson,
        string? AfterJson,
        string ChangedFieldsJson);

    private sealed record CanonicalAncestor(string Name, string? ParentKey);
    private sealed record ExistingAncestor(string Name, Guid? ParentId);

    private static TerritorialCatalogUnitDto ToCatalogUnit(TerritorialUnitRecord x) => new(
        x.Id, x.CountryId, x.Country.Name, x.TerritorialUnitTypeId, x.TerritorialUnitType.Code, x.TerritorialUnitType.Name,
        x.ParentId, x.Parent?.Names.FirstOrDefault(n => n.IsPrimary)?.Name,
        x.Codes.FirstOrDefault(c => c.IsPrimary)?.Value ?? x.Codes.FirstOrDefault()?.Value,
        x.Names.FirstOrDefault(n => n.IsPrimary)?.Name ?? x.Names.FirstOrDefault()?.Name ?? string.Empty,
        x.Names.FirstOrDefault(n => n.IsPrimary)?.Locale, x.Latitude, x.Longitude, x.IsActive,
        x.ManualSelectableLocality ?? x.TerritorialUnitType.IsSelectableLocality,
        x.HasManualActiveOverride || x.ManualSelectableLocality != null || x.HasManualCoordinateOverride,
        x.HasManualActiveOverride, x.ManualSelectableLocality != null, x.HasManualCoordinateOverride);

    private static IQueryable<TerritorialCatalogHierarchyNodeDto> CatalogHierarchyNodes(IQueryable<TerritorialUnitRecord> query) =>
        query.Select(x => new TerritorialCatalogHierarchyNodeDto(
            x.Id, x.CountryId, x.ParentId, x.TerritorialUnitTypeId, x.TerritorialUnitType.Code, x.TerritorialUnitType.Name,
            x.Names.Where(n => n.IsPrimary).Select(n => n.Name).FirstOrDefault() ?? x.Names.Select(n => n.Name).FirstOrDefault() ?? string.Empty,
            x.Codes.Where(c => c.IsPrimary).Select(c => c.Value).FirstOrDefault() ?? x.Codes.Select(c => c.Value).FirstOrDefault(),
            x.IsActive, x.ManualSelectableLocality ?? x.TerritorialUnitType.IsSelectableLocality, x.Children.Count));

    private static TerritorialCatalogUnitDto ToCatalogUnit(CatalogDescendantRow x) => new(
        x.Id, x.CountryId, x.Country, x.TerritorialUnitTypeId, x.TypeCode, x.Type, x.ParentId, x.Parent,
        x.PrimaryCode, x.PrimaryName, x.Locale, x.Latitude, x.Longitude, x.IsActive, x.IsSelectableLocality,
        x.HasManualOverride, x.HasManualActiveOverride, x.HasManualSelectableOverride, x.HasManualCoordinateOverride);

    private sealed class CatalogPathRow
    {
        public Guid Id { get; init; }
        public int Depth { get; init; }
    }

    private sealed class CatalogDescendantTypeRow
    {
        public Guid TerritorialUnitTypeId { get; init; }
        public string TypeCode { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public int Count { get; init; }
        public bool IsSelectableLocality { get; init; }
    }

    private sealed class CatalogDescendantRow
    {
        public Guid Id { get; init; }
        public Guid CountryId { get; init; }
        public string Country { get; init; } = string.Empty;
        public Guid TerritorialUnitTypeId { get; init; }
        public string TypeCode { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public Guid? ParentId { get; init; }
        public string? Parent { get; init; }
        public string? PrimaryCode { get; init; }
        public string PrimaryName { get; init; } = string.Empty;
        public string? Locale { get; init; }
        public decimal? Latitude { get; init; }
        public decimal? Longitude { get; init; }
        public bool IsActive { get; init; }
        public bool IsSelectableLocality { get; init; }
        public bool HasManualOverride { get; init; }
        public bool HasManualActiveOverride { get; init; }
        public bool HasManualSelectableOverride { get; init; }
        public bool HasManualCoordinateOverride { get; init; }
        public int TotalCount { get; init; }
    }
}
