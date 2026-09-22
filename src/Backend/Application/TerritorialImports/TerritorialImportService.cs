using System.Security.Cryptography;
using System.Text.Json;
using Zuppeto.Domain.TerritorialImports;

namespace Zuppeto.Application.TerritorialImports;

public sealed class TerritorialImportService(
    ITerritorialWorkbookReader reader,
    ITerritorialImportAuthorizer authorizer,
    ITerritorialImportStore store,
    ITerritorialCatalogImportGateway catalog,
    TerritorialMappingEngine mapper,
    IEnumerable<ITerritorialCanonicalizer> canonicalizers,
    TerritorialImportValidator validator,
    TerritorialDiffEngine diffEngine)
{
    public async Task<PreparedTerritorialImport> PrepareAsync(PrepareTerritorialImportRequest request, CancellationToken cancellationToken = default)
    {
        await authorizer.EnsureAdminAsync(request.ActorUserId, cancellationToken);
        await using var buffer = new MemoryStream();
        await request.Artifact.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var checksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;
        var import = new TerritorialImport(Guid.NewGuid(), request.DatasetSourceId, request.MappingTemplateId,
            request.ArtifactName, checksum, bytes.LongLength, request.DatasetVersion, request.ActorUserId, now);
        await store.CreateAsync(import, cancellationToken);

        try
        {
            var template = await store.GetMappingAsync(request.MappingTemplateId, cancellationToken)
                ?? throw new InvalidOperationException("No s'ha trobat el mapping territorial.");
            if (!template.IsActive || template.DatasetSourceId != request.DatasetSourceId)
                throw new InvalidOperationException("El mapping no és actiu o no correspon a la font.");

            var definition = JsonSerializer.Deserialize<TerritorialMappingDefinition>(template.DefinitionJson, TerritorialImportJson.Options)
                ?? throw new InvalidOperationException("La definició del mapping no és vàlida.");
            buffer.Position = 0;
            var workbook = await reader.ReadAsync(buffer, cancellationToken);
            if (!string.Equals(template.SchemaFingerprint, workbook.SchemaFingerprint, StringComparison.Ordinal))
                throw new InvalidOperationException("El fingerprint de l'esquema XLSX no coincideix amb el mapping.");

            var rows = mapper.Map(workbook, definition);
            import.MarkMapped(DateTimeOffset.UtcNow);
            await store.SaveMappedAsync(import, rows, cancellationToken);

            var canonicalizerKey = definition.Canonicalizer ?? "default";
            var canonicalizer = canonicalizers.SingleOrDefault(item => item.Key == canonicalizerKey)
                ?? throw new InvalidOperationException($"No existeix el canonicalitzador '{canonicalizerKey}'.");
            var canonical = canonicalizer.Canonicalize(rows);
            var context = await catalog.GetContextAsync(request.DatasetSourceId, cancellationToken);
            var issues = canonical.Issues.Concat(validator.Validate(canonical.Units, context)).ToList();
            import.MarkValidated(issues.Any(item => item.Severity == TerritorialIssueSeverity.Error), DateTimeOffset.UtcNow);
            await store.SaveValidatedAsync(import, issues, cancellationToken);

            if (import.HasBlockingErrors) return new PreparedTerritorialImport(import, rows, canonical.Units, issues, null);

            var managedSchemes = definition.Sheets.SelectMany(sheet => sheet.Units).SelectMany(unit => unit.Codes).Select(code => code.Scheme).Distinct().ToArray();
            var diff = diffEngine.Build(import.Id, context, canonical.Units, definition.DeactivationGuard, DateTimeOffset.UtcNow, managedSchemes);
            issues.AddRange(diff.Issues);
            if (diff.Issues.Any(item => item.Severity == TerritorialIssueSeverity.Error))
            {
                import.RecordBlockingErrors(DateTimeOffset.UtcNow);
                await store.SaveValidatedAsync(import, diff.Issues, cancellationToken);
                return new PreparedTerritorialImport(import, rows, canonical.Units, issues, null);
            }

            import.MarkReadyForReview(context.CatalogVersion, DateTimeOffset.UtcNow);
            await store.SaveReadyAsync(import, diff.ChangeSet, cancellationToken);
            return new PreparedTerritorialImport(import, rows, canonical.Units, issues, diff.ChangeSet);
        }
        catch (Exception exception)
        {
            import.Fail(exception.Message, DateTimeOffset.UtcNow);
            await store.MarkFailedAsync(import, cancellationToken);
            throw;
        }
    }

    public async Task PublishAsync(Guid importId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await authorizer.EnsureAdminAsync(actorUserId, cancellationToken);
        await catalog.PublishAsync(importId, actorUserId, cancellationToken);
    }

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
