using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuppeto.Application.TerritorialImports;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;
using Zuppeto.Infrastructure.TerritorialImports;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class TerritorialImportPersistenceTests
{
    [Fact]
    public async Task PostgreSql_pipeline_persists_preview_publishes_idempotently_and_reverts_latest()
    {
        var connectionString = Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        var testConnection = new NpgsqlConnectionStringBuilder(connectionString) { Database = "zuppeto_territorial_phase5_tests" }.ConnectionString;
        var options = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(testConnection).Options;
        await using var db = new ZuppetoDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var country = new CountryRecord { Id = Guid.NewGuid(), Code = "test-country", Name = "País", Iso2 = "PT", Iso3 = "PRT", IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now };
            var actor = new UserRecord { Id = Guid.NewGuid(), Email = $"territorial-{Guid.NewGuid():N}@example.test", Role = "Admin", PrivacyAccepted = true, PrivacyAcceptedAtUtc = now, CreatedAtUtc = now };
            var source = new TerritorialDatasetSourceRecord
            {
                Id = Guid.NewGuid(), CountryId = country.Id, Organisation = "Official", Dataset = "Pilot", Url = "https://example.test",
                License = "Open", Attribution = "Official", CommercialUseAllowed = true, TransformationAllowed = true,
                ApprovalStatus = "Approved", VerifiedAtUtc = now, VerifiedByUserId = actor.Id, IsActive = true,
                PublicationMode = "FullSnapshot", DatasetVersion = "v1", CreatedAtUtc = now, UpdatedAtUtc = now
            };
            var type = new TerritorialUnitTypeRecord { Id = Guid.NewGuid(), CountryId = country.Id, Code = "MUNICIPALITY", Name = "Municipi", DisplayOrder = 1, IsSelectableLocality = true, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now };
            var definition = new TerritorialMappingDefinition([
                new TerritorialSheetMappingDefinition("Data", 1, [
                    new TerritorialUnitProjectionDefinition("MUNICIPALITY",
                        new(MappingValueOperation.Concat, Parts: [new(MappingValueOperation.Constant, Constant: "unit:"), new(MappingValueOperation.Column, Column: "CODE")]),
                        null, new(MappingValueOperation.Column, Column: "NAME"), "pt-PT", "Official",
                        [new("test:code", new(MappingValueOperation.Column, Column: "CODE"), true)])
                ])]);
            var definitionJson = JsonSerializer.Serialize(definition, TerritorialImportJson.Options);
            var mapping = new TerritorialMappingTemplateRecord
            {
                Id = Guid.NewGuid(), DatasetSourceId = source.Id, Version = 1, DefinitionJson = definitionJson,
                SchemaFingerprint = "schema", DefinitionChecksum = new string('a', 64), IsActive = true, CreatedAtUtc = now
            };
            db.AddRange(country, actor, source, type, mapping);
            await db.SaveChangesAsync();

            var workbook = new TerritorialSourceWorkbook([new TerritorialSourceSheet("Data", [
                new TerritorialSourceRow(1, new Dictionary<string, string?> { ["A"] = "CODE", ["B"] = "NAME" }),
                new TerritorialSourceRow(2, new Dictionary<string, string?> { ["A"] = "001", ["B"] = "Àgueda" })
            ])], "schema");
            var store = new TerritorialImportStore(db);
            var gateway = new TerritorialCatalogImportGateway(db);
            var service = new TerritorialImportService(new FixedWorkbookReader(workbook), new TerritorialImportAuthorizer(db), store, gateway,
                new TerritorialMappingEngine(), [new DefaultTerritorialCanonicalizer()], new TerritorialImportValidator(), new TerritorialDiffEngine());

            var first = await service.PrepareAsync(Request(source.Id, mapping.Id, actor.Id, "first"));
            Assert.Equal("ReadyForReview", first.Import.Status.ToString());
            Assert.NotNull(first.ChangeSet);
            await service.PublishAsync(first.Import.Id, actor.Id);
            db.ChangeTracker.Clear();

            Assert.Equal("Published", await db.TerritorialImports.Where(x => x.Id == first.Import.Id).Select(x => x.Status).SingleAsync());
            Assert.Equal(1, await db.TerritorialCatalogStates.Where(x => x.CountryId == country.Id).Select(x => x.Version).SingleAsync());
            Assert.Equal("Àgueda", await db.TerritorialUnitNames.Select(x => x.Name).SingleAsync());

            var duplicate = await service.PrepareAsync(Request(source.Id, mapping.Id, actor.Id, "first"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.PublishAsync(duplicate.Import.Id, actor.Id));

            await service.RevertLatestAsync(first.Import.Id, actor.Id);
            db.ChangeTracker.Clear();
            Assert.Equal("Reverted", await db.TerritorialImports.Where(x => x.Id == first.Import.Id).Select(x => x.Status).SingleAsync());
            Assert.False(await db.TerritorialUnits.Select(x => x.IsActive).SingleAsync());
            Assert.Equal(2, await db.TerritorialCatalogStates.Where(x => x.CountryId == country.Id).Select(x => x.Version).SingleAsync());
            Assert.Equal(2, await db.TerritorialChangeSets.CountAsync(x => x.ImportId == first.Import.Id));
            Assert.Equal(1, await db.TerritorialUnitCodes.CountAsync());

            var concurrentA = await service.PrepareAsync(Request(source.Id, mapping.Id, actor.Id, "second", "v2"));
            var concurrentB = await service.PrepareAsync(Request(source.Id, mapping.Id, actor.Id, "third", "v3"));
            await service.PublishAsync(concurrentA.Import.Id, actor.Id);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => service.PublishAsync(concurrentB.Import.Id, actor.Id));
            await service.RevertLatestAsync(concurrentA.Import.Id, actor.Id);
            db.ChangeTracker.Clear();
            Assert.Equal(4, await db.TerritorialCatalogStates.Where(x => x.CountryId == country.Id).Select(x => x.Version).SingleAsync());

            await db.TerritorialDatasetSources.Where(x => x.Id == source.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.ApprovalStatus, "Pending"));
            var unapproved = await service.PrepareAsync(Request(source.Id, mapping.Id, actor.Id, "fourth", "v4"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.PublishAsync(unapproved.Import.Id, actor.Id));
            Assert.Equal(4, await db.TerritorialCatalogStates.Where(x => x.CountryId == country.Id).Select(x => x.Version).SingleAsync());
            await service.CancelAsync(unapproved.Import.Id, actor.Id);
            Assert.Equal("Cancelled", await db.TerritorialImports.Where(x => x.Id == unapproved.Import.Id).Select(x => x.Status).SingleAsync());

            var nonAdmin = new UserRecord { Id = Guid.NewGuid(), Email = $"territorial-user-{Guid.NewGuid():N}@example.test", Role = "User", PrivacyAccepted = true, PrivacyAcceptedAtUtc = now, CreatedAtUtc = now };
            db.Users.Add(nonAdmin);
            await db.SaveChangesAsync();
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.PrepareAsync(Request(source.Id, mapping.Id, nonAdmin.Id, "forbidden", "v5")));
            Assert.False(await db.TerritorialImports.AnyAsync(x => x.CreatedByUserId == nonAdmin.Id));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    private static PrepareTerritorialImportRequest Request(Guid sourceId, Guid mappingId, Guid actorId, string content, string version = "v1") =>
        new(sourceId, mappingId, "pilot.xlsx", version, new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)), actorId);

    private sealed class FixedWorkbookReader(TerritorialSourceWorkbook workbook) : ITerritorialWorkbookReader
    {
        public Task<TerritorialSourceWorkbook> ReadAsync(Stream source, CancellationToken cancellationToken = default) => Task.FromResult(workbook);
    }
}
