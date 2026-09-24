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
    TerritorialDiffEngine diffEngine)
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
            request.ArtifactName, checksum, bytes.LongLength, request.DatasetVersion, request.ActorUserId, now);
        await store.CreateAsync(import, bytes, cancellationToken);
        return import.Id;
    }

    public async Task ProcessPreparationAsync(TerritorialImportWorkItem work, CancellationToken cancellationToken = default)
    {
        var import = new TerritorialImport(work.ImportId, work.DatasetSourceId, work.MappingTemplateId,
            work.ArtifactName, work.FileChecksum, work.FileSize, work.DatasetVersion, work.ActorUserId, DateTimeOffset.UtcNow);
        import.MarkUploaded(DateTimeOffset.UtcNow);
        await store.BeginPreparationAsync(import, cancellationToken);
        await store.EnsureNotCancelledAsync(import.Id, cancellationToken);
        var template = await store.GetMappingAsync(work.MappingTemplateId, cancellationToken)
            ?? throw new InvalidOperationException("No s'ha trobat el mapping territorial.");
        if (!template.IsActive || template.DatasetSourceId != work.DatasetSourceId)
            throw new InvalidOperationException("El mapping no és actiu o no correspon a la font.");

        var definition = JsonSerializer.Deserialize<TerritorialMappingDefinition>(template.DefinitionJson, TerritorialImportJson.Options)
            ?? throw new InvalidOperationException("La definició del mapping no és vàlida.");
        await store.SetStageAsync(import.Id, TerritorialImportStage.Reading, cancellationToken: cancellationToken);
        await using var artifact = await store.OpenArtifactAsync(import.Id, cancellationToken);
        var workbook = await reader.ReadAsync(artifact, cancellationToken);
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
