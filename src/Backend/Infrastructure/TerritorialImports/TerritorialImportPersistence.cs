using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.TerritorialImports;
using Zuppeto.Domain.Geography;
using Zuppeto.Domain.TerritorialImports;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.TerritorialImports;

internal sealed class TerritorialImportAuthorizer(ZuppetoDbContext db) : ITerritorialImportAuthorizer
{
    public async Task EnsureAdminAsync(Guid actorUserId, CancellationToken ct = default)
    {
        if (actorUserId == Guid.Empty || !await db.Users.AsNoTracking()
                .AnyAsync(x => x.Id == actorUserId && x.Role == "Admin", ct))
            throw new UnauthorizedAccessException("L'operació d'importació territorial requereix un ADMIN autoritzat.");
    }
}

internal sealed class TerritorialImportStore(ZuppetoDbContext db) : ITerritorialImportStore
{
    public async Task<TerritorialMappingTemplate?> GetMappingAsync(Guid id, CancellationToken ct = default)
    {
        var item = await db.TerritorialMappingTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? null : new TerritorialMappingTemplate(item.Id, item.DatasetSourceId, item.Version,
            item.DefinitionJson, item.SchemaFingerprint, item.DefinitionChecksum, item.CreatedAtUtc, item.IsActive);
    }

    public async Task CreateAsync(TerritorialImport import, CancellationToken ct = default)
    {
        var mode = await db.TerritorialDatasetSources.Where(x => x.Id == import.DatasetSourceId).Select(x => x.PublicationMode).SingleAsync(ct);
        db.TerritorialImports.Add(new TerritorialImportRecord
        {
            Id = import.Id, DatasetSourceId = import.DatasetSourceId, MappingTemplateId = import.MappingTemplateId,
            ArtifactName = import.ArtifactName, FileChecksum = import.FileChecksum, FileSize = import.FileSize,
            DatasetVersion = import.DatasetVersion, PublicationMode = mode, Status = import.Status.ToString(),
            CreatedByUserId = import.CreatedByUserId, CreatedAtUtc = import.CreatedAtUtc, UpdatedAtUtc = import.UpdatedAtUtc
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveMappedAsync(TerritorialImport import, IReadOnlyCollection<TerritorialMappedRow> rows, CancellationToken ct = default)
    {
        db.TerritorialImportRows.AddRange(rows.Select(row => new TerritorialImportRowRecord
        {
            Id = Guid.NewGuid(), ImportId = import.Id, Sheet = row.Sheet, RowNumber = row.RowNumber,
            SourceJson = JsonSerializer.Serialize(row.SourceValues, TerritorialImportJson.Options),
            CanonicalJson = JsonSerializer.Serialize(row.Candidate, TerritorialImportJson.Options),
            CanonicalUnitKey = row.Candidate.CanonicalUnitKey, ParentCanonicalUnitKey = row.Candidate.ParentCanonicalUnitKey
        }));
        await UpdateImport(import, new { rows = rows.Count }, ct);
    }

    public async Task SaveValidatedAsync(TerritorialImport import, IReadOnlyCollection<TerritorialImportIssue> issues, CancellationToken ct = default)
    {
        db.TerritorialImportIssues.AddRange(issues.Select(issue => new TerritorialImportIssueRecord
        {
            Id = Guid.NewGuid(), ImportId = import.Id, RuleCode = issue.RuleCode, Severity = issue.Severity.ToString(), Message = issue.Message,
            Sheet = issue.Sheet, RowNumber = issue.RowNumber, Field = issue.Field,
            ProblemValue = Truncate(issue.ProblemValue, 500), CanonicalUnitKey = issue.CanonicalUnitKey, CreatedAtUtc = DateTimeOffset.UtcNow
        }));
        await UpdateImport(import, new { errors = issues.Count(x => x.Severity == TerritorialIssueSeverity.Error), warnings = issues.Count(x => x.Severity == TerritorialIssueSeverity.Warning) }, ct);
    }

    public async Task SaveReadyAsync(TerritorialImport import, TerritorialChangeSet changeSet, CancellationToken ct = default)
    {
        db.TerritorialChangeSets.Add(new TerritorialChangeSetRecord
        {
            Id = changeSet.Id, ImportId = import.Id, CountryId = changeSet.CountryId, CatalogVersion = changeSet.CatalogVersion,
            Status = changeSet.Status.ToString(), CreatedAtUtc = changeSet.CreatedAtUtc,
            Items = changeSet.Changes.Select(change => new TerritorialChangeSetItemRecord
            {
                Id = change.Id, Kind = change.Kind.ToString(), TerritorialUnitId = change.TerritorialUnitId,
                CanonicalUnitKey = change.CanonicalUnitKey, BeforeJson = change.BeforeJson, AfterJson = change.AfterJson,
                ChangedFieldsJson = JsonSerializer.Serialize(change.ChangedFields, TerritorialImportJson.Options)
            }).ToList()
        });
        await UpdateImport(import, new { changes = changeSet.Changes.Count, actionable = changeSet.Changes.Count(x => x.Kind != TerritorialChangeKind.NoChange) }, ct);
    }

    public Task MarkFailedAsync(TerritorialImport import, CancellationToken ct = default) => UpdateImport(import, new { }, ct);

    public async Task CancelAsync(Guid importId, CancellationToken ct = default)
    {
        var record = await db.TerritorialImports.SingleAsync(x => x.Id == importId, ct);
        if (record.Status is "Published" or "Reverted")
            throw new InvalidOperationException("Una importació publicada no es pot cancel·lar.");
        record.Status = "Cancelled";
        record.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task UpdateImport(TerritorialImport import, object summary, CancellationToken ct)
    {
        var record = await db.TerritorialImports.SingleAsync(x => x.Id == import.Id, ct);
        record.Status = import.Status.ToString(); record.HasBlockingErrors = import.HasBlockingErrors;
        record.CatalogVersion = import.CatalogVersion; record.UpdatedAtUtc = import.UpdatedAtUtc;
        record.PublishedAtUtc = import.PublishedAtUtc; record.FailureReason = Truncate(import.FailureReason, 2000);
        record.SummaryJson = JsonSerializer.Serialize(summary, TerritorialImportJson.Options);
        await db.SaveChangesAsync(ct);
    }

    private static string? Truncate(string? value, int length) => value is null || value.Length <= length ? value : value[..length];
}

internal sealed class TerritorialCatalogImportGateway(ZuppetoDbContext db) : ITerritorialCatalogImportGateway
{
    public async Task<TerritorialImportContext> GetContextAsync(Guid sourceId, CancellationToken ct = default)
    {
        var source = await db.TerritorialDatasetSources.AsNoTracking().SingleAsync(x => x.Id == sourceId, ct);
        var version = await db.TerritorialCatalogStates.AsNoTracking().Where(x => x.CountryId == source.CountryId).Select(x => (long?)x.Version).SingleOrDefaultAsync(ct) ?? 0;
        var types = await db.TerritorialUnitTypes.AsNoTracking().Where(x => x.CountryId == source.CountryId).Select(x => x.Code).ToArrayAsync(ct);
        var units = await db.TerritorialUnits.AsNoTracking().Where(x => x.CountryId == source.CountryId)
            .Include(x => x.TerritorialUnitType).Include(x => x.Names).Include(x => x.Codes).ToArrayAsync(ct);
        return new TerritorialImportContext(source.Id, source.CountryId, source.ApprovalStatus, source.PublicationMode, version, types,
            units.Select(Snapshot).ToArray());
    }

    public async Task PublishAsync(Guid importId, Guid actorUserId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var import = await db.TerritorialImports.Include(x => x.DatasetSource).Include(x => x.ChangeSets).ThenInclude(x => x.Items)
            .SingleAsync(x => x.Id == importId, ct);
        var changeSet = import.ChangeSets.SingleOrDefault(x => x.RevertsChangeSetId == null);
        if (import.Status != "ReadyForReview" || changeSet is null || import.HasBlockingErrors) throw new InvalidOperationException("La importació no està preparada per publicar.");
        var sourceGate = await db.TerritorialDatasetSources.AsNoTracking().Where(x => x.Id == import.DatasetSourceId)
            .Select(x => new { x.IsActive, x.ApprovalStatus }).SingleAsync(ct);
        if (!sourceGate.IsActive || sourceGate.ApprovalStatus != "Approved") throw new InvalidOperationException("La font territorial no està aprovada.");
        if (actorUserId == Guid.Empty) throw new InvalidOperationException("L'actor de publicació és obligatori.");
        if (await db.TerritorialImports.AnyAsync(x => x.Id != import.Id && x.DatasetSourceId == import.DatasetSourceId &&
                x.DatasetVersion == import.DatasetVersion && x.FileChecksum == import.FileChecksum && x.Status == "Published", ct))
            throw new InvalidOperationException("Aquest artefacte i versió ja s'han publicat.");

        var state = await db.TerritorialCatalogStates.AsNoTracking().SingleOrDefaultAsync(x => x.CountryId == import.DatasetSource.CountryId, ct);
        var publishedAtUtc = DateTimeOffset.UtcNow;
        if (state is null)
        {
            if (import.CatalogVersion != 0) throw new DbUpdateConcurrencyException("La versió del catàleg ha canviat.");
            db.TerritorialCatalogStates.Add(new TerritorialCatalogStateRecord
            {
                CountryId = import.DatasetSource.CountryId,
                Version = 1,
                UpdatedAtUtc = publishedAtUtc
            });
        }
        else
        {
            if (state.Version != import.CatalogVersion) throw new DbUpdateConcurrencyException("El preview territorial ha quedat obsolet.");
            var updatedStates = await db.TerritorialCatalogStates
                .Where(x => x.CountryId == import.DatasetSource.CountryId && x.Version == import.CatalogVersion)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(x => x.Version, x => x.Version + 1)
                    .SetProperty(x => x.UpdatedAtUtc, publishedAtUtc), ct);
            if (updatedStates != 1) throw new DbUpdateConcurrencyException("El preview territorial ha quedat obsolet.");
        }

        await ApplyItems(changeSet.Items, import.DatasetSource, ct);
        import.Status = "Published"; import.PublishedAtUtc = publishedAtUtc; import.UpdatedAtUtc = publishedAtUtc;
        changeSet.Status = "Published"; changeSet.PublishedAtUtc = import.PublishedAtUtc;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            var records = string.Join(", ", exception.Entries.Select(x => x.Metadata.ClrType.Name));
            throw new DbUpdateConcurrencyException($"El catàleg territorial ha canviat mentre es publicava ({records}).", exception);
        }
        await transaction.CommitAsync(ct);
    }

    public async Task RevertLatestAsync(Guid importId, Guid actorUserId, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var import = await db.TerritorialImports.Include(x => x.DatasetSource).Include(x => x.ChangeSets).ThenInclude(x => x.Items)
            .SingleAsync(x => x.Id == importId, ct);
        var changeSet = import.ChangeSets.SingleOrDefault(x => x.RevertsChangeSetId == null);
        if (import.Status != "Published" || changeSet is null || actorUserId == Guid.Empty) throw new InvalidOperationException("La publicació no es pot revertir.");
        var state = await db.TerritorialCatalogStates.AsNoTracking().SingleAsync(x => x.CountryId == import.DatasetSource.CountryId, ct);
        if (state.Version != changeSet.CatalogVersion + 1) throw new InvalidOperationException("Només es pot revertir l'última publicació del país.");
        var revertedAtUtc = DateTimeOffset.UtcNow;
        var updatedStates = await db.TerritorialCatalogStates
            .Where(x => x.CountryId == import.DatasetSource.CountryId && x.Version == state.Version)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.Version, x => x.Version + 1)
                .SetProperty(x => x.UpdatedAtUtc, revertedAtUtc), ct);
        if (updatedStates != 1) throw new DbUpdateConcurrencyException("El catàleg ha canviat durant la reversió.");

        foreach (var item in changeSet.Items.Reverse())
        {
            if (item.Kind == "Create" && item.TerritorialUnitId is not null)
            {
                var created = await db.TerritorialUnits.SingleOrDefaultAsync(x => x.Id == item.TerritorialUnitId, ct);
                if (created is not null) created.IsActive = false;
            }
            else if (item.BeforeJson is not null && item.TerritorialUnitId is not null)
            {
                var before = JsonSerializer.Deserialize<TerritorialCatalogUnitSnapshot>(item.BeforeJson, TerritorialImportJson.Options)!;
                var unit = await LoadUnit(item.TerritorialUnitId.Value, ct);
                unit.TerritorialUnitTypeId = await db.TerritorialUnitTypes
                    .Where(x => x.CountryId == import.DatasetSource.CountryId && x.Code == before.TerritorialUnitTypeCode)
                    .Select(x => x.Id).SingleAsync(ct);
                RestoreSnapshot(unit, before, import.DatasetSourceId);
            }
        }

        var reversal = new TerritorialChangeSetRecord
        {
            Id = Guid.NewGuid(), ImportId = import.Id, CountryId = import.DatasetSource.CountryId, CatalogVersion = state.Version,
            Status = "Published", RevertsChangeSetId = changeSet.Id, CreatedAtUtc = revertedAtUtc, PublishedAtUtc = revertedAtUtc,
            Items = changeSet.Items.Select(item => new TerritorialChangeSetItemRecord
            {
                Id = Guid.NewGuid(), Kind = item.Kind, TerritorialUnitId = item.TerritorialUnitId, CanonicalUnitKey = item.CanonicalUnitKey,
                BeforeJson = item.AfterJson, AfterJson = item.BeforeJson, ChangedFieldsJson = item.ChangedFieldsJson
            }).ToList()
        };
        db.TerritorialChangeSets.Add(reversal);
        import.Status = "Reverted"; changeSet.Status = "Reverted";
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            var records = string.Join(", ", exception.Entries.Select(x => x.Metadata.ClrType.Name));
            throw new DbUpdateConcurrencyException($"El catàleg territorial ha canviat mentre es revertia ({records}).", exception);
        }
        await transaction.CommitAsync(ct);
    }

    private async Task ApplyItems(ICollection<TerritorialChangeSetItemRecord> items, TerritorialDatasetSourceRecord source, CancellationToken ct)
    {
        var actionable = items.Where(x => x.Kind != "NoChange").ToArray();
        var canonical = actionable.Where(x => x.AfterJson is not null).ToDictionary(x => x.CanonicalUnitKey,
            x => JsonSerializer.Deserialize<CanonicalTerritorialUnit>(x.AfterJson!, TerritorialImportJson.Options)!, StringComparer.Ordinal);
        var ids = items.Where(x => x.TerritorialUnitId is not null).ToDictionary(x => x.CanonicalUnitKey, x => x.TerritorialUnitId!.Value, StringComparer.Ordinal);
        foreach (var item in actionable.Where(x => x.Kind == "Create"))
        {
            var id = Guid.NewGuid(); item.TerritorialUnitId = id; ids[item.CanonicalUnitKey] = id;
        }
        var types = await db.TerritorialUnitTypes.Where(x => x.CountryId == source.CountryId).ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, ct);

        foreach (var item in actionable)
        {
            if (item.Kind == "Deactivate")
            {
                var existing = await db.TerritorialUnits.SingleAsync(x => x.Id == item.TerritorialUnitId, ct); existing.IsActive = false; existing.UpdatedAtUtc = DateTimeOffset.UtcNow; continue;
            }
            var model = canonical[item.CanonicalUnitKey];
            Guid? parentId = model.ParentCanonicalUnitKey is null ? null : ids.GetValueOrDefault(model.ParentCanonicalUnitKey);
            if (model.ParentCanonicalUnitKey is not null && parentId is null) throw new InvalidOperationException("No s'ha pogut resoldre el pare durant la publicació.");
            var type = types.GetValueOrDefault(model.TerritorialUnitTypeCode) ?? throw new InvalidOperationException("Tipus territorial desconegut.");
            ValidateDomain(model, source.CountryId, type.Id, parentId, source.Id);
            TerritorialUnitRecord unit;
            if (item.Kind == "Create")
            {
                unit = new TerritorialUnitRecord { Id = item.TerritorialUnitId!.Value, CountryId = source.CountryId, CreatedAtUtc = DateTimeOffset.UtcNow };
                db.TerritorialUnits.Add(unit);
            }
            else unit = await LoadUnit(item.TerritorialUnitId!.Value, ct);
            ApplyCanonical(unit, model, type.Id, parentId, source.Id);
        }
    }

    private async Task<TerritorialUnitRecord> LoadUnit(Guid id, CancellationToken ct) => await db.TerritorialUnits.Include(x => x.Names).Include(x => x.Codes).SingleAsync(x => x.Id == id, ct);

    private static void ApplyCanonical(TerritorialUnitRecord unit, CanonicalTerritorialUnit model, Guid typeId, Guid? parentId, Guid sourceId)
    {
        unit.TerritorialUnitTypeId = typeId; unit.ParentId = parentId; unit.IsActive = model.IsActive;
        unit.Latitude = model.Latitude; unit.Longitude = model.Longitude; unit.CoordinateSourceId = model.Latitude is null ? null : model.CoordinateSourceId ?? sourceId; unit.UpdatedAtUtc = DateTimeOffset.UtcNow;
        if (!NamesMatch(unit.Names, model.Names, sourceId))
        {
            unit.Names.Clear(); foreach (var name in model.Names) unit.Names.Add(new TerritorialUnitNameRecord { Id = Guid.NewGuid(), Name = name.Name, Locale = name.Locale, Kind = name.Kind, IsPrimary = name.IsPrimary, NormalizedName = TerritorialNameNormalizer.Normalize(name.Name), DatasetSourceId = name.DatasetSourceId ?? sourceId, CreatedAtUtc = DateTimeOffset.UtcNow });
        }
        if (!CodesMatch(unit.Codes, model.Codes, sourceId))
        {
            unit.Codes.Clear(); foreach (var code in model.Codes) unit.Codes.Add(new TerritorialUnitCodeRecord { Id = Guid.NewGuid(), Scheme = code.Scheme, Value = code.Value, IsPrimary = code.IsPrimary, ValidFrom = code.ValidFrom, ValidTo = code.ValidTo, DatasetSourceId = code.DatasetSourceId ?? sourceId, CreatedAtUtc = DateTimeOffset.UtcNow });
        }
    }

    private static void RestoreSnapshot(TerritorialUnitRecord unit, TerritorialCatalogUnitSnapshot before, Guid fallbackSourceId)
    {
        unit.ParentId = before.ParentId; unit.IsActive = before.IsActive; unit.Latitude = before.Latitude; unit.Longitude = before.Longitude; unit.CoordinateSourceId = before.CoordinateSourceId;
        if (!NamesMatch(unit.Names, before.Names, fallbackSourceId))
        {
            unit.Names.Clear(); foreach (var name in before.Names) unit.Names.Add(new TerritorialUnitNameRecord { Id = Guid.NewGuid(), Name = name.Name, Locale = name.Locale, Kind = name.Kind, IsPrimary = name.IsPrimary, NormalizedName = TerritorialNameNormalizer.Normalize(name.Name), DatasetSourceId = name.DatasetSourceId ?? fallbackSourceId, CreatedAtUtc = DateTimeOffset.UtcNow });
        }
        if (!CodesMatch(unit.Codes, before.Codes, fallbackSourceId))
        {
            unit.Codes.Clear(); foreach (var code in before.Codes) unit.Codes.Add(new TerritorialUnitCodeRecord { Id = Guid.NewGuid(), Scheme = code.Scheme, Value = code.Value, IsPrimary = code.IsPrimary, ValidFrom = code.ValidFrom, ValidTo = code.ValidTo, DatasetSourceId = code.DatasetSourceId ?? fallbackSourceId, CreatedAtUtc = DateTimeOffset.UtcNow });
        }
        unit.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static bool NamesMatch(ICollection<TerritorialUnitNameRecord> current, IReadOnlyCollection<CanonicalTerritorialName> incoming, Guid sourceId) =>
        current.Count == incoming.Count && incoming.All(item => current.Any(existing =>
            existing.Name == item.Name && existing.Locale == item.Locale && existing.Kind == item.Kind &&
            existing.IsPrimary == item.IsPrimary && existing.DatasetSourceId == (item.DatasetSourceId ?? sourceId)));

    private static bool CodesMatch(ICollection<TerritorialUnitCodeRecord> current, IReadOnlyCollection<CanonicalTerritorialCode> incoming, Guid sourceId) =>
        current.Count == incoming.Count && incoming.All(item => current.Any(existing =>
            existing.Scheme == item.Scheme && existing.Value == item.Value && existing.IsPrimary == item.IsPrimary &&
            existing.ValidFrom == item.ValidFrom && existing.ValidTo == item.ValidTo &&
            existing.DatasetSourceId == (item.DatasetSourceId ?? sourceId)));

    private static void ValidateDomain(CanonicalTerritorialUnit model, Guid countryId, Guid typeId, Guid? parentId, Guid sourceId)
    {
        var unit = new TerritorialUnit(Guid.NewGuid(), countryId, typeId, parentId, model.IsActive);
        foreach (var name in model.Names) unit.AddName(new TerritorialUnitName(Guid.NewGuid(), name.Name, Enum.Parse<TerritorialNameKind>(name.Kind), name.Locale, name.IsPrimary, name.DatasetSourceId ?? sourceId));
        foreach (var code in model.Codes) unit.AddCode(new TerritorialUnitCode(Guid.NewGuid(), code.Scheme, code.Value, code.ValidFrom, code.ValidTo, code.IsPrimary, code.DatasetSourceId ?? sourceId));
        if (model.Latitude is not null && model.Longitude is not null) unit.SetCoordinates(model.Latitude.Value, model.Longitude.Value, model.CoordinateSourceId ?? sourceId, zeroZeroWasVerified: false);
    }

    private static TerritorialCatalogUnitSnapshot Snapshot(TerritorialUnitRecord unit) => new(unit.Id, unit.TerritorialUnitType.Code, unit.ParentId,
        unit.Names.Select(x => new CanonicalTerritorialName(x.Name, x.Locale, x.Kind, x.IsPrimary, x.DatasetSourceId)).ToArray(),
        unit.Codes.Select(x => new CanonicalTerritorialCode(x.Scheme, x.Value, x.IsPrimary, x.ValidFrom, x.ValidTo, x.DatasetSourceId)).ToArray(),
        unit.Latitude, unit.Longitude, unit.IsActive, unit.CoordinateSourceId);
}
