using System.Security.Cryptography;
using System.Text.Json;
using Zuppeto.Domain.TerritorialImports;

namespace Zuppeto.Application.TerritorialImports;

public sealed class TerritorialImportService(
    ITerritorialWorkbookReader reader,
    ITerritorialImportAuthorizer authorizer,
    ITerritorialImportStore store,
    ITerritorialImportWorkQueue workQueue,
    ITerritorialCatalogImportGateway catalog,
    TerritorialMappingEngine mapper,
    IEnumerable<ITerritorialCanonicalizer> canonicalizers,
    TerritorialImportValidator validator,
    TerritorialDiffEngine diffEngine,
    ITerritorialNormalizedContractInterpreter normalizedInterpreter,
    ITerritorialContractSourceResolver sourceResolver)
{
    public async Task<Guid> QueueAsync(PrepareTerritorialImportRequest request, CancellationToken cancellationToken = default)
    {
        await authorizer.EnsureAdminAsync(request.ActorUserId, cancellationToken);
        await using var buffer = new MemoryStream();
        await request.Artifact.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var checksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;
        var import = new TerritorialImport(Guid.NewGuid(), request.DatasetSourceId, request.MappingTemplateId,
            request.ArtifactName, checksum, bytes.LongLength, request.DatasetVersion, request.ActorUserId, now,
            request.InputKind, request.ContractVersion, request.ImportMode, request.FieldScope);
        await store.CreateAsync(import, bytes, request.Sources, cancellationToken);
        return import.Id;
    }

    public async Task ProcessPreparationAsync(TerritorialImportWorkItem work, CancellationToken cancellationToken = default)
    {
        var import = new TerritorialImport(work.ImportId, work.DatasetSourceId, work.MappingTemplateId,
            work.ArtifactName, work.FileChecksum, work.FileSize, work.DatasetVersion, work.ActorUserId, DateTimeOffset.UtcNow,
            work.InputKind, work.ContractVersion, work.ImportMode, work.FieldScope);
        import.MarkUploaded(DateTimeOffset.UtcNow);
        await store.BeginPreparationAsync(import, cancellationToken);
        await store.EnsureNotCancelledAsync(import.Id, cancellationToken);
        await store.SetStageAsync(import.Id, TerritorialImportStage.Reading, cancellationToken: cancellationToken);
        await using var artifact = await store.OpenArtifactAsync(import.Id, cancellationToken);
        var workbook = await reader.ReadAsync(artifact, cancellationToken);

        if (work.InputKind == TerritorialInputKind.PetilocNormalized)
        {
            await ProcessNormalizedAsync(import, work, workbook, cancellationToken);
            return;
        }

        var template = await store.GetMappingAsync(work.MappingTemplateId!.Value, cancellationToken)
            ?? throw new InvalidOperationException("No s'ha trobat el mapping territorial.");
        if (!template.IsActive || template.DatasetSourceId != work.DatasetSourceId)
            throw new InvalidOperationException("El mapping no és actiu o no correspon a la font.");

        var definition = JsonSerializer.Deserialize<TerritorialMappingDefinition>(template.DefinitionJson, TerritorialImportJson.Options)
            ?? throw new InvalidOperationException("La definició del mapping no és vàlida.");
        if (!string.Equals(template.SchemaFingerprint, workbook.SchemaFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("El fingerprint de l'esquema XLSX no coincideix amb el mapping.");

        await store.EnsureNotCancelledAsync(import.Id, cancellationToken);
        var rows = mapper.Map(workbook, definition);
        await store.SetStageAsync(import.Id, TerritorialImportStage.Staging, rows.Count, 0, cancellationToken);
        import.MarkMapped(DateTimeOffset.UtcNow);
        await store.SaveMappedAsync(import, rows, cancellationToken);

        await store.EnsureNotCancelledAsync(import.Id, cancellationToken);
        await store.SetStageAsync(import.Id, TerritorialImportStage.Canonicalization, rows.Count, rows.Count, cancellationToken);
        var canonicalizerKey = definition.Canonicalizer ?? "default";
        var canonicalizer = canonicalizers.SingleOrDefault(item => item.Key == canonicalizerKey)
            ?? throw new InvalidOperationException($"No existeix el canonicalitzador '{canonicalizerKey}'.");
        var canonical = canonicalizer.Canonicalize(rows);
        await store.EnsureNotCancelledAsync(import.Id, cancellationToken);
        await store.SetStageAsync(import.Id, TerritorialImportStage.Validation, rows.Count, rows.Count, cancellationToken);
        var context = await catalog.GetContextAsync(work.DatasetSourceId, cancellationToken);
        var issues = canonical.Issues.Concat(validator.Validate(canonical.Units, context)).ToList();
        import.MarkValidated(issues.Any(item => item.Severity == TerritorialIssueSeverity.Error), DateTimeOffset.UtcNow);
        await store.SaveValidatedAsync(import, issues, cancellationToken);

        if (import.HasBlockingErrors) return;

        await store.EnsureNotCancelledAsync(import.Id, cancellationToken);
        await store.SetStageAsync(import.Id, TerritorialImportStage.ChangeSet, rows.Count, rows.Count, cancellationToken);
        var managedSchemes = definition.Sheets.SelectMany(sheet => sheet.Units).SelectMany(unit => unit.Codes).Select(code => code.Scheme).Distinct().ToArray();
        var diff = diffEngine.Build(import.Id, context, canonical.Units, definition.DeactivationGuard, DateTimeOffset.UtcNow, managedSchemes);
        issues.AddRange(diff.Issues);
        if (diff.Issues.Any(item => item.Severity == TerritorialIssueSeverity.Error))
        {
            import.RecordBlockingErrors(DateTimeOffset.UtcNow);
            await store.SaveValidatedAsync(import, diff.Issues, cancellationToken);
            return;
        }

        await store.EnsureNotCancelledAsync(import.Id, cancellationToken);
        import.MarkReadyForReview(context.CatalogVersion, DateTimeOffset.UtcNow);
        await store.SaveReadyAsync(import, diff.ChangeSet, cancellationToken);
    }

    private async Task ProcessNormalizedAsync(TerritorialImport import, TerritorialImportWorkItem work,
        TerritorialSourceWorkbook workbook, CancellationToken cancellationToken)
    {
        if (!normalizedInterpreter.IsNormalized(workbook))
            throw new InvalidDataException("L'artefacte declarat com a contracte normalitzat no conté Manifest.");

        var contract = normalizedInterpreter.Interpret(workbook);
        var issues = contract.Issues.ToList();
        if (contract.Manifest.ContractVersion != work.ContractVersion || contract.Manifest.ImportMode != work.ImportMode)
            issues.Add(new TerritorialImportIssue("CONTRACT_METADATA_MISMATCH", TerritorialIssueSeverity.Error,
                "Les metadades del contracte no coincideixen amb les registrades en posar la importació a la cua."));
        var contributingKeys = ContributingSourceKeys(contract);
        var resolved = await sourceResolver.ResolveAsync(contract.Manifest.CountryIso2,
            contract.Sources.Select(source => source.SourceKey).ToArray(), contributingKeys, cancellationToken);
        var sources = resolved.Sources.ToDictionary(source => source.SourceKey, StringComparer.Ordinal);
        var context = await catalog.GetContextAsync(work.DatasetSourceId, cancellationToken);
        if (context.CountryId != resolved.CountryId)
            issues.Add(new TerritorialImportIssue("COUNTRY_MISMATCH", TerritorialIssueSeverity.Error,
                "La font principal no pertany al país declarat al Manifest."));

        if (contract.Manifest.ImportMode == TerritorialImportMode.Patch &&
            contract.Manifest.FieldScope.Any(field => field != "coordinates"))
            issues.Add(new TerritorialImportIssue("PATCH_SCOPE_NOT_IMPLEMENTED", TerritorialIssueSeverity.Error,
                "El contracte 1.0 implementa de forma publicable únicament PATCH de coordinates."));

        var rows = new List<TerritorialMappedRow>();
        var units = new List<CanonicalTerritorialUnit>();
        var coordinatePatches = new List<(CanonicalTerritorialUnit Unit, TerritorialCatalogUnitSnapshot Current, TerritorialFieldOperation Operation)>();
        if (contract.Manifest.ImportMode == TerritorialImportMode.Full)
        {
            foreach (var source in contract.Units)
            {
                var identityScheme = TerritorialIdentityScheme.Canonicalize(
                    contract.Manifest.CountryIso2, source.IdentityScheme, source.IdentityValue);
                var names = new List<CanonicalTerritorialName>();
                if (!string.IsNullOrWhiteSpace(source.PrimaryName))
                    names.Add(new CanonicalTerritorialName(source.PrimaryName, source.PrimaryLocale, "Official", true,
                        SourceId(sources, source.StructureSourceKey)));
                names.AddRange(contract.Names.Where(name => name.UnitKey == source.UnitKey && name.Operation == TerritorialFieldOperation.Set)
                    .Select(name => new CanonicalTerritorialName(name.Name, name.Locale, name.Kind, name.IsPrimary, SourceId(sources, name.SourceKey))));
                var codes = new List<CanonicalTerritorialCode>
                {
                    new(identityScheme, source.IdentityValue, true, DatasetSourceId: SourceId(sources, source.StructureSourceKey))
                };
                codes.AddRange(contract.Codes.Where(code => code.UnitKey == source.UnitKey && code.Operation == TerritorialFieldOperation.Set)
                    .Select(code => new CanonicalTerritorialCode(code.Scheme, code.Value, code.IsPrimary, code.ValidFrom, code.ValidTo, SourceId(sources, code.SourceKey))));
                var unit = new CanonicalTerritorialUnit(source.UnitKey, source.ParentUnitKey, source.TypeCode ?? string.Empty,
                    names, codes, source.Latitude, source.Longitude, source.IsActive ?? true,
                    SourceId(sources, source.CoordinateSourceKey), SourceId(sources, source.StructureSourceKey));
                units.Add(unit);
                rows.Add(ToMappedRow(source, unit));
            }
        }
        else
        {
            foreach (var source in contract.Units)
            {
                var identityScheme = TerritorialIdentityScheme.Canonicalize(
                    contract.Manifest.CountryIso2, source.IdentityScheme, source.IdentityValue);
                var matches = context.CurrentUnits.Where(current => current.Codes.Any(code =>
                    code.Scheme == identityScheme && code.Value == source.IdentityValue)).ToArray();
                if (matches.Length != 1)
                {
                    issues.Add(new TerritorialImportIssue(matches.Length == 0 ? "IDENTITY_NOT_FOUND" : "IDENTITY_AMBIGUOUS",
                        TerritorialIssueSeverity.Error,
                        matches.Length == 0 ? "La identitat del PATCH no existeix al catàleg." : "La identitat del PATCH coincideix amb més d'una unitat.",
                        "Unitats", source.RowNumber, "identity_value", source.IdentityValue, source.UnitKey));
                    continue;
                }
                var current = matches[0];
                var unit = new CanonicalTerritorialUnit(source.UnitKey, null, current.TerritorialUnitTypeCode,
                    current.Names, current.Codes,
                    source.CoordinateOperation == TerritorialFieldOperation.Keep ? current.Latitude : source.Latitude,
                    source.CoordinateOperation == TerritorialFieldOperation.Keep ? current.Longitude : source.Longitude,
                    current.IsActive,
                    source.CoordinateOperation == TerritorialFieldOperation.Keep ? current.CoordinateSourceId :
                        source.CoordinateOperation == TerritorialFieldOperation.Clear ? null : SourceId(sources, source.CoordinateSourceKey));
                units.Add(unit);
                rows.Add(ToMappedRow(source, unit));
                coordinatePatches.Add((unit, current, source.CoordinateOperation));
            }
        }

        await store.SetStageAsync(import.Id, TerritorialImportStage.Staging, contract.Units.Count, rows.Count, cancellationToken);
        import.MarkMapped(DateTimeOffset.UtcNow);
        await store.SaveMappedAsync(import, rows, cancellationToken);
        await store.SetStageAsync(import.Id, TerritorialImportStage.Validation, contract.Units.Count, rows.Count, cancellationToken);
        if (contract.Manifest.ImportMode == TerritorialImportMode.Full)
            issues.AddRange(validator.Validate(units, context));
        import.MarkValidated(issues.Any(issue => issue.Severity == TerritorialIssueSeverity.Error), DateTimeOffset.UtcNow);
        await store.SaveValidatedAsync(import, issues, cancellationToken);
        if (import.HasBlockingErrors) return;

        await store.SetStageAsync(import.Id, TerritorialImportStage.ChangeSet, contract.Units.Count, rows.Count, cancellationToken);
        var diff = contract.Manifest.ImportMode == TerritorialImportMode.Patch
            ? diffEngine.BuildCoordinatePatch(import.Id, context, coordinatePatches, DateTimeOffset.UtcNow)
            : diffEngine.Build(import.Id, context, units, null, DateTimeOffset.UtcNow,
                units.SelectMany(unit => unit.Codes).Select(code => code.Scheme).Distinct().ToArray());
        issues.AddRange(diff.Issues);
        if (diff.Issues.Any(issue => issue.Severity == TerritorialIssueSeverity.Error))
        {
            import.RecordBlockingErrors(DateTimeOffset.UtcNow);
            await store.SaveValidatedAsync(import, diff.Issues, cancellationToken);
            return;
        }
        import.MarkReadyForReview(context.CatalogVersion, DateTimeOffset.UtcNow);
        await store.SaveReadyAsync(import, diff.ChangeSet, cancellationToken);
    }

    private static TerritorialMappedRow ToMappedRow(TerritorialContractUnit source, CanonicalTerritorialUnit unit) =>
        new("Unitats", source.RowNumber, new Dictionary<string, string?>
        {
            ["unit_key"] = source.UnitKey, ["identity_scheme"] = source.IdentityScheme,
            ["identity_value"] = source.IdentityValue
        }, unit, new Dictionary<string, TerritorialFieldOperation> { ["coordinates"] = source.CoordinateOperation });

    private static Guid? SourceId(IReadOnlyDictionary<string, TerritorialImportSourceReference> sources, string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : sources.GetValueOrDefault(key)?.DatasetSourceId;

    private static IReadOnlyCollection<string> ContributingSourceKeys(TerritorialNormalizedContract contract) =>
        contract.Units.Select(unit => unit.StructureSourceKey)
            .Concat(contract.Units.Where(unit => unit.CoordinateOperation != TerritorialFieldOperation.Keep).Select(unit => unit.CoordinateSourceKey))
            .Concat(contract.Names.Where(name => name.Operation != TerritorialFieldOperation.Keep).Select(name => name.SourceKey))
            .Concat(contract.Codes.Where(code => code.Operation != TerritorialFieldOperation.Keep).Select(code => code.SourceKey))
            .Where(key => !string.IsNullOrWhiteSpace(key)).Select(key => key!).Distinct(StringComparer.Ordinal).ToArray();

    public async Task PublishAsync(Guid importId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, cancellationToken);
        await workQueue.QueuePublicationAsync(importId, actorUserId, cancellationToken);
    }

    public Task ProcessPublicationAsync(TerritorialImportWorkItem work, CancellationToken cancellationToken = default) =>
        catalog.PublishAsync(work.ImportId, work.ActorUserId, cancellationToken);

    public async Task RevertLatestAsync(Guid importId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, cancellationToken);
        await catalog.RevertLatestAsync(importId, actorUserId, cancellationToken);
    }

    public async Task CancelAsync(Guid importId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, cancellationToken);
        await store.CancelAsync(importId, cancellationToken);
    }
}
