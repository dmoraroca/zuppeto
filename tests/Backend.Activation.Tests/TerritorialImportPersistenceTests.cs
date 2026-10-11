using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuppeto.Application.TerritorialImports;
using Zuppeto.Domain.TerritorialImports;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;
using Zuppeto.Infrastructure.TerritorialImports;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class TerritorialImportPersistenceTests
{
    [Fact]
    public async Task PostgreSql_normalized_coordinate_patch_is_reviewable_but_pending_source_blocks_publication()
    {
        var connectionString = Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        var testConnection = new NpgsqlConnectionStringBuilder(connectionString) { Database = "zuppeto_territorial_phase8_tests" }.ConnectionString;
        var options = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(testConnection).Options;
        await using var db = new ZuppetoDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var country = new CountryRecord { Id = Guid.NewGuid(), Code = "ES", Name = "Espanya", Iso2 = "ES", Iso3 = "ESP", IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now };
            var actor = new UserRecord { Id = Guid.NewGuid(), Email = $"phase8-{Guid.NewGuid():N}@example.test", Role = "Admin", PrivacyAccepted = true, PrivacyAcceptedAtUtc = now, CreatedAtUtc = now };
            var ine = Source(country.Id, actor.Id, "ES-INE-30247", "INE", "Approved", now);
            var cnig = Source(country.Id, actor.Id, "CNIG_NGMEP_2026", "CNIG", "Pending", now);
            cnig.VerifiedAtUtc = null; cnig.VerifiedByUserId = null;
            var type = new TerritorialUnitTypeRecord { Id = Guid.NewGuid(), CountryId = country.Id, Code = "MUNICIPALITY", Name = "Municipi", DisplayOrder = 1, IsSelectableLocality = true, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now };
            var unit = new TerritorialUnitRecord { Id = Guid.NewGuid(), CountryId = country.Id, TerritorialUnitTypeId = type.Id, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now,
                Names = [new() { Id = Guid.NewGuid(), Name = "Arenys de Mar", NormalizedName = "arenys de mar", Locale = "ca-ES", Kind = "Official", IsPrimary = true, DatasetSourceId = ine.Id, CreatedAtUtc = now }],
                Codes = [new() { Id = Guid.NewGuid(), Scheme = "es:ine:municipality", Value = "08006", IsPrimary = true, DatasetSourceId = ine.Id, CreatedAtUtc = now }] };
            db.AddRange(country, actor, ine, cnig, type, unit);
            await db.SaveChangesAsync();

            var workbook = NormalizedCoordinatePatch();
            var store = new TerritorialImportStore(db);
            var queue = new TerritorialImportWorkQueue(db);
            var gateway = new TerritorialCatalogImportGateway(db);
            var service = new TerritorialImportService(new FixedWorkbookReader(workbook), new TerritorialImportAuthorizer(db), store, queue, gateway,
                new TerritorialMappingEngine(), [new DefaultTerritorialCanonicalizer()], new TerritorialImportValidator(), new TerritorialDiffEngine(),
                new PetilocTerritorialContractInterpreter(), new TerritorialContractSourceResolver(db));
            var request = new PrepareTerritorialImportRequest(cnig.Id, null, "patch.xlsx", "2026-coordinates", new MemoryStream([1]), actor.Id,
                TerritorialInputKind.PetilocNormalized, "1.0", TerritorialImportMode.Patch, ["coordinates"],
                [new(ine.Id, ine.SourceKey, ine.ApprovalStatus, true, false), new(cnig.Id, cnig.SourceKey, cnig.ApprovalStatus, true, true)]);
            var importId = await Prepare(service, queue, request);
            Assert.Equal("ReadyForReview", await Status(db, importId));
            var changes = await db.TerritorialChangeSetItems.Where(item => item.ChangeSet.ImportId == importId).ToArrayAsync();
            var change = Assert.Single(changes);
            Assert.Equal("Update", change.Kind);
            Assert.Contains("coordinates", change.ChangedFieldsJson);

            var adminRepository = new TerritorialAdminRepository(db);
            Assert.False((await adminRepository.GetImportAsync(importId))!.CanPublish);
            var incomplete = Source(country.Id, actor.Id, "INCOMPLETE-TEST", "Incomplete", "Pending", now);
            incomplete.License = null; incomplete.VerifiedAtUtc = null; incomplete.VerifiedByUserId = null;
            db.TerritorialDatasetSources.Add(incomplete);
            await db.SaveChangesAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => adminRepository.DecideSourceAsync(incomplete.Id, actor.Id,
                new("approve", "Pending", "Intent d'aprovació amb metadades incompletes.")));
            Assert.Equal("Pending", await db.TerritorialDatasetSources.Where(item => item.Id == incomplete.Id).Select(item => item.ApprovalStatus).SingleAsync());
            var approved = await adminRepository.DecideSourceAsync(cnig.Id, actor.Id,
                new("approve", "Pending", "Llicència i procedència verificades en una base aïllada."));
            Assert.Equal("Approved", approved.ApprovalStatus);
            Assert.True((await adminRepository.GetImportAsync(importId))!.CanPublish);
            Assert.Equal("ReadyForReview", await Status(db, importId));
            Assert.Single(await db.TerritorialSourceApprovalAudit.Where(item => item.DatasetSourceId == cnig.Id).ToArrayAsync());
            await using (var restartedDb = new ZuppetoDbContext(options))
            {
                Assert.Equal("Approved", await restartedDb.TerritorialDatasetSources.Where(item => item.Id == cnig.Id)
                    .Select(item => item.ApprovalStatus).SingleAsync());
                Assert.Single(await restartedDb.TerritorialSourceApprovalAudit.Where(item => item.DatasetSourceId == cnig.Id).ToArrayAsync());
            }
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => adminRepository.DecideSourceAsync(cnig.Id, actor.Id,
                new("reject", "Pending", "Decisió concurrent obsoleta.")));
            db.ChangeTracker.Clear();
            var rejected = await adminRepository.DecideSourceAsync(cnig.Id, actor.Id,
                new("reject", "Approved", "Rebuig simulat per conservar el bloqueig de publicació."));
            Assert.Equal("Rejected", rejected.ApprovalStatus);
            Assert.False((await adminRepository.GetImportAsync(importId))!.CanPublish);
            Assert.Equal(2, await db.TerritorialSourceApprovalAudit.CountAsync(item => item.DatasetSourceId == cnig.Id));

            await service.PublishAsync(importId, actor.Id);
            var publication = await queue.ClaimNextAsync("test", TimeSpan.FromMinutes(1)) ?? throw new InvalidOperationException();
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessPublicationAsync(publication));
            db.ChangeTracker.Clear();
            var preserved = await db.TerritorialUnits.AsNoTracking().SingleAsync(item => item.Id == unit.Id);
            Assert.Null(preserved.Latitude);
            Assert.Null(preserved.Longitude);
            Assert.Equal("Arenys de Mar", await db.TerritorialUnitNames.Where(item => item.TerritorialUnitId == unit.Id).Select(item => item.Name).SingleAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    [Fact]
    public async Task PostgreSql_cnig_seed_is_idempotent_and_preserves_human_approval()
    {
        var connectionString = Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        var testConnection = new NpgsqlConnectionStringBuilder(connectionString) { Database = "zuppeto_territorial_cnig_seed_tests" }.ConnectionString;
        var options = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(testConnection).Options;
        await using var db = new ZuppetoDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        try
        {
            var seeder = new TerritorialPilotConfigurationSeeder(db);
            await seeder.SeedAsync();
            var source = await db.TerritorialDatasetSources.SingleAsync(item => item.SourceKey == "CNIG_NGMEP_2026");
            Assert.Equal("Pending", source.ApprovalStatus);
            Assert.True(source.IsActive);
            Assert.Equal(new DateOnly(2026, 1, 1), source.DatasetDate);
            Assert.Equal("Obra derivada de NGMEP CC-BY 4.0 ign.es", source.Attribution);
            Assert.Contains("CC BY 4.0", source.License);
            Assert.DoesNotContain(await db.TerritorialDatasetSources.ToListAsync(), item => item.SourceKey == "ES-CNIG-NGBE");
            var resolution = await new TerritorialContractSourceResolver(db).ResolveAsync(
                "ES", ["ES-INE-30247", "CNIG_NGMEP_2026"], ["CNIG_NGMEP_2026"]);
            Assert.Equal(2, resolution.Sources.Count);
            var resolvedCnig = Assert.Single(resolution.Sources, item => item.SourceKey == "CNIG_NGMEP_2026");
            Assert.Equal(source.Id, resolvedCnig.DatasetSourceId);
            Assert.Equal("Pending", resolvedCnig.ApprovalStatus);
            Assert.True(resolvedCnig.IsActive);
            Assert.True(resolvedCnig.ContributesPublishedFields);

            var actor = new UserRecord
            {
                Id = Guid.NewGuid(), Email = $"cnig-approval-{Guid.NewGuid():N}@example.test", Role = "Admin",
                PrivacyAccepted = true, PrivacyAcceptedAtUtc = DateTimeOffset.UtcNow, CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Users.Add(actor);
            source.SourceKey = "ES-CNIG-NGBE";
            source.ApprovalStatus = "Approved";
            source.VerifiedAtUtc = DateTimeOffset.UtcNow;
            source.VerifiedByUserId = actor.Id;
            await db.SaveChangesAsync();

            await seeder.SeedAsync();
            db.ChangeTracker.Clear();
            var reseeded = await db.TerritorialDatasetSources.SingleAsync(item => item.SourceKey == "CNIG_NGMEP_2026");
            Assert.Equal("Approved", reseeded.ApprovalStatus);
            Assert.Equal(actor.Id, reseeded.VerifiedByUserId);
            Assert.Equal(new DateOnly(2026, 1, 1), reseeded.DatasetDate);
            Assert.Equal(1, await db.TerritorialDatasetSources.CountAsync(item => item.Country.Iso2 == "ES" && item.Dataset == "Nomenclátor Geográfico de Municipios y Entidades de Población"));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    [Fact]
    public async Task PostgreSql_real_germany_contract_prepares_15770_creates_without_touching_spain_and_pending_blocks_publication()
    {
        var connectionString = Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        var testConnection = new NpgsqlConnectionStringBuilder(connectionString) { Database = "zuppeto_territorial_germany_phase8_tests" }.ConnectionString;
        var options = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(testConnection).Options;
        await using var db = new ZuppetoDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        try
        {
            await new TerritorialPilotConfigurationSeeder(db).SeedAsync();
            var now = DateTimeOffset.UtcNow;
            var actor = new UserRecord
            {
                Id = Guid.NewGuid(), Email = $"germany-phase8-{Guid.NewGuid():N}@example.test", Role = "Admin",
                PrivacyAccepted = true, PrivacyAcceptedAtUtc = now, CreatedAtUtc = now
            };
            var spain = await db.Countries.SingleAsync(country => country.Iso2 == "ES");
            var spainType = await db.TerritorialUnitTypes.SingleAsync(type => type.CountryId == spain.Id && type.Code == "MUNICIPALITY");
            var spainSource = await db.TerritorialDatasetSources.SingleAsync(source => source.SourceKey == "ES-INE-30247");
            var spanishUnit = new TerritorialUnitRecord
            {
                Id = Guid.NewGuid(), CountryId = spain.Id, TerritorialUnitTypeId = spainType.Id, IsActive = true,
                CreatedAtUtc = now, UpdatedAtUtc = now,
                Names = [new() { Id = Guid.NewGuid(), Name = "Test Espanya", NormalizedName = "test espanya", Locale = "ca-ES", Kind = "Official", IsPrimary = true, DatasetSourceId = spainSource.Id, CreatedAtUtc = now }],
                Codes = [new() { Id = Guid.NewGuid(), Scheme = "es:ine:municipality", Value = "00000", IsPrimary = true, DatasetSourceId = spainSource.Id, CreatedAtUtc = now }]
            };
            db.AddRange(actor, spanishUnit);
            await db.SaveChangesAsync();

            var germanySource = await db.TerritorialDatasetSources.SingleAsync(source => source.SourceKey == "DE-DESTATIS-GVISYS");
            Assert.Equal("Pending", germanySource.ApprovalStatus);
            Assert.True(germanySource.IsActive);
            Assert.True(germanySource.CommercialUseAllowed);
            Assert.True(germanySource.TransformationAllowed);
            await new TerritorialPilotConfigurationSeeder(db).SeedAsync();
            db.ChangeTracker.Clear();
            germanySource = await db.TerritorialDatasetSources.SingleAsync(source => source.SourceKey == "DE-DESTATIS-GVISYS");
            Assert.Equal("Pending", germanySource.ApprovalStatus);
            Assert.Equal(1, await db.TerritorialDatasetSources.CountAsync(source => source.SourceKey == "DE-DESTATIS-GVISYS"));

            var store = new TerritorialImportStore(db);
            var queue = new TerritorialImportWorkQueue(db);
            var gateway = new TerritorialCatalogImportGateway(db);
            var service = new TerritorialImportService(new XlsxTerritorialReader(), new TerritorialImportAuthorizer(db), store, queue, gateway,
                new TerritorialMappingEngine(), [new DefaultTerritorialCanonicalizer(), new GvIsysCanonicalizer(new DefaultTerritorialCanonicalizer())],
                new TerritorialImportValidator(), new TerritorialDiffEngine(), new PetilocTerritorialContractInterpreter(), new TerritorialContractSourceResolver(db));
            var filePath = Path.Combine(FindRepositoryRoot(), "docs", "idiomes", "municipis per pais", "PetiLoc_Alemanya_20260930_Contracte_1_0.xlsx");
            await using var artifact = File.OpenRead(filePath);
            var request = new PrepareTerritorialImportRequest(germanySource.Id, null, Path.GetFileName(filePath), "2026-Q3", artifact, actor.Id,
                TerritorialInputKind.PetilocNormalized, "1.0", TerritorialImportMode.Full, [],
                [new(germanySource.Id, germanySource.SourceKey, germanySource.ApprovalStatus, true, true)]);
            var importId = await Prepare(service, queue, request);

            Assert.Equal("ReadyForReview", await Status(db, importId));
            Assert.Equal(15770, await db.TerritorialImportRows.CountAsync(row => row.ImportId == importId));
            Assert.Equal(15770, await db.TerritorialChangeSetItems.CountAsync(item => item.ChangeSet.ImportId == importId && item.Kind == "Create"));
            Assert.False(await db.TerritorialImportIssues.AnyAsync(issue => issue.ImportId == importId));
            Assert.Equal(0, await db.TerritorialUnits.CountAsync(unit => unit.CountryId == germanySource.CountryId));
            Assert.Equal(1, await db.TerritorialUnits.CountAsync(unit => unit.CountryId == spain.Id));
            Assert.Equal("Test Espanya", await db.TerritorialUnitNames.Where(name => name.TerritorialUnitId == spanishUnit.Id).Select(name => name.Name).SingleAsync());

            await service.PublishAsync(importId, actor.Id);
            var publication = await queue.ClaimNextAsync("test", TimeSpan.FromMinutes(1)) ?? throw new InvalidOperationException();
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessPublicationAsync(publication));
            db.ChangeTracker.Clear();
            Assert.Equal("Pending", await db.TerritorialDatasetSources.Where(source => source.Id == germanySource.Id).Select(source => source.ApprovalStatus).SingleAsync());
            Assert.Equal(0, await db.TerritorialUnits.CountAsync(unit => unit.CountryId == germanySource.CountryId));
            Assert.Equal(1, await db.TerritorialUnits.CountAsync(unit => unit.CountryId == spain.Id));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

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
            var queue = new TerritorialImportWorkQueue(db);
            var gateway = new TerritorialCatalogImportGateway(db);
            var service = new TerritorialImportService(new FixedWorkbookReader(workbook), new TerritorialImportAuthorizer(db), store, queue, gateway,
                new TerritorialMappingEngine(), [new DefaultTerritorialCanonicalizer()], new TerritorialImportValidator(), new TerritorialDiffEngine(),
                new PetilocTerritorialContractInterpreter(), new TerritorialContractSourceResolver(db));

            var firstId = await Prepare(service, queue, Request(source.Id, mapping.Id, actor.Id, "first"));
            Assert.Equal("ReadyForReview", await Status(db, firstId));
            Assert.True(await db.TerritorialChangeSets.AnyAsync(x => x.ImportId == firstId));
            var adminRepository = new TerritorialAdminRepository(db);
            var functionalChanges = await adminRepository.ListChangesAsync(firstId, new TerritorialChangeQuery(null, "Àgueda"));
            var functionalChange = Assert.Single(functionalChanges.Items);
            Assert.Equal("Create", functionalChange.Kind);
            Assert.Equal("Àgueda", functionalChange.Name);
            Assert.Equal("001", functionalChange.PrimaryCode);
            Assert.Equal("MUNICIPALITY", functionalChange.TerritorialUnitTypeCode);
            Assert.Equal("Municipi", functionalChange.TerritorialUnitType);
            Assert.Equal("País", functionalChange.Country);
            Assert.Equal("Official · Pilot", functionalChange.Source);
            Assert.Equal(["País", "Àgueda"], functionalChange.Hierarchy);
            Assert.Equal("Official", functionalChange.Provenance.Organisation);
            Assert.Equal("Pilot", functionalChange.Provenance.Dataset);
            Assert.Equal("v1", functionalChange.Provenance.DatasetVersion);
            Assert.Equal(1, functionalChange.Provenance.MappingVersion);
            Assert.Equal("https://example.test", functionalChange.Provenance.Source);
            Assert.Contains("encara no existeix", functionalChange.FunctionalReason);
            var importDetail = await adminRepository.GetImportAsync(firstId);
            Assert.NotNull(importDetail);
            Assert.Equal(["Data"], importDetail.SourceSheets);
            Assert.Equal(0, importDetail.ManualConflictCount);
            var publicationBreakdown = Assert.Single(importDetail.TerritorialBreakdown);
            Assert.Equal("MUNICIPALITY", publicationBreakdown.TerritorialUnitTypeCode);
            Assert.Equal("Municipi", publicationBreakdown.TerritorialUnitType);
            Assert.Equal(1, publicationBreakdown.Create);
            Assert.Single((await adminRepository.ListChangesAsync(firstId, new TerritorialChangeQuery(null, "001"))).Items);
            Assert.Single((await adminRepository.ListSourcePreviewAsync(firstId, new TerritorialPreviewQuery(null, "Àgueda"))).Items);
            var cleanCanonicalRow = Assert.Single((await adminRepository.ListCanonicalPreviewAsync(firstId, new TerritorialPreviewQuery(null, "Àgueda"))).Items);
            Assert.Equal("Vàlida", cleanCanonicalRow.Status);
            Assert.Equal(0, cleanCanonicalRow.IssueCount);
            Assert.Empty(cleanCanonicalRow.Issues);
            db.TerritorialImportIssues.Add(new TerritorialImportIssueRecord
            {
                Id = Guid.NewGuid(), ImportId = firstId, RuleCode = "CONTROLLED_WARNING", Severity = "Warning",
                Message = "Avís funcional controlat", Sheet = "Data", RowNumber = 2, Field = "NAME",
                CanonicalUnitKey = "unit:001", CreatedAtUtc = now
            });
            await db.SaveChangesAsync();
            var warnedCanonicalRow = Assert.Single((await adminRepository.ListCanonicalPreviewAsync(firstId, new TerritorialPreviewQuery(null, "Àgueda"))).Items);
            Assert.Equal("Vàlida", warnedCanonicalRow.Status);
            Assert.Equal(1, warnedCanonicalRow.IssueCount);
            var warning = Assert.Single(warnedCanonicalRow.Issues);
            Assert.Equal("CONTROLLED_WARNING", warning.Rule);
            Assert.Equal("Avís funcional controlat", warning.Message);
            await Publish(service, queue, firstId, actor.Id);
            db.ChangeTracker.Clear();

            Assert.Equal("Published", await Status(db, firstId));
            Assert.Equal(1, await db.TerritorialCatalogStates.Where(x => x.CountryId == country.Id).Select(x => x.Version).SingleAsync());
            Assert.Equal("Àgueda", await db.TerritorialUnitNames.Select(x => x.Name).SingleAsync());

            var duplicateId = await Prepare(service, queue, Request(source.Id, mapping.Id, actor.Id, "first"));
            await service.PublishAsync(duplicateId, actor.Id);
            var duplicateWork = await queue.ClaimNextAsync("test", TimeSpan.FromMinutes(1)) ?? throw new InvalidOperationException();
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessPublicationAsync(duplicateWork));

            await service.RevertLatestAsync(firstId, actor.Id);
            db.ChangeTracker.Clear();
            Assert.Equal("Reverted", await Status(db, firstId));
            Assert.False(await db.TerritorialUnits.Select(x => x.IsActive).SingleAsync());
            Assert.Equal(2, await db.TerritorialCatalogStates.Where(x => x.CountryId == country.Id).Select(x => x.Version).SingleAsync());
            Assert.Equal(2, await db.TerritorialChangeSets.CountAsync(x => x.ImportId == firstId));
            Assert.Equal(1, await db.TerritorialUnitCodes.CountAsync());

            var concurrentA = await Prepare(service, queue, Request(source.Id, mapping.Id, actor.Id, "second", "v2"));
            var concurrentB = await Prepare(service, queue, Request(source.Id, mapping.Id, actor.Id, "third", "v3"));
            await Publish(service, queue, concurrentA, actor.Id);
            await service.PublishAsync(concurrentB, actor.Id);
            var staleWork = await queue.ClaimNextAsync("test", TimeSpan.FromMinutes(1)) ?? throw new InvalidOperationException();
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => service.ProcessPublicationAsync(staleWork));
            await service.RevertLatestAsync(concurrentA, actor.Id);
            db.ChangeTracker.Clear();
            Assert.Equal(4, await db.TerritorialCatalogStates.Where(x => x.CountryId == country.Id).Select(x => x.Version).SingleAsync());

            await db.TerritorialDatasetSources.Where(x => x.Id == source.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.ApprovalStatus, "Pending"));
            var unapproved = await Prepare(service, queue, Request(source.Id, mapping.Id, actor.Id, "fourth", "v4"));
            await service.PublishAsync(unapproved, actor.Id);
            var unapprovedWork = await queue.ClaimNextAsync("test", TimeSpan.FromMinutes(1)) ?? throw new InvalidOperationException();
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessPublicationAsync(unapprovedWork));
            Assert.Equal(4, await db.TerritorialCatalogStates.Where(x => x.CountryId == country.Id).Select(x => x.Version).SingleAsync());
            await queue.FailAsync(unapproved, "test", "EXPECTED", "expected", false, 3);
            await service.CancelAsync(unapproved, actor.Id);
            Assert.Equal("Cancelled", await Status(db, unapproved));

            var restartId = await service.QueueAsync(Request(source.Id, mapping.Id, actor.Id, "restart", "v6"));
            Assert.True(await db.TerritorialImportArtifacts.AnyAsync(x => x.ImportId == restartId));
            var abandoned = await queue.ClaimNextAsync("worker-before-restart", TimeSpan.Zero);
            Assert.Equal(restartId, abandoned?.ImportId);
            db.ChangeTracker.Clear();
            var reclaimed = await queue.ClaimNextAsync("worker-after-restart", TimeSpan.FromMinutes(1));
            Assert.Equal(restartId, reclaimed?.ImportId);
            Assert.Equal(2, await db.TerritorialImports.Where(x => x.Id == restartId).Select(x => x.AttemptCount).SingleAsync());

            await service.CancelAsync(restartId, actor.Id);
            Assert.True(await db.TerritorialImports.Where(x => x.Id == restartId).Select(x => x.CancellationRequested).SingleAsync());
            await Assert.ThrowsAsync<TerritorialImportCancellationException>(() => service.ProcessPreparationAsync(reclaimed!));
            await queue.CancelClaimedAsync(restartId, "worker-after-restart");
            Assert.Equal("Cancelled", await Status(db, restartId));

            var exclusiveId = await service.QueueAsync(Request(source.Id, mapping.Id, actor.Id, "exclusive", "v7"));
            var exclusive = await queue.ClaimNextAsync("worker-a", TimeSpan.FromMinutes(1));
            Assert.Equal(exclusiveId, exclusive?.ImportId);
            Assert.Null(await queue.ClaimNextAsync("worker-b", TimeSpan.FromMinutes(1)));
            await queue.CancelClaimedAsync(exclusiveId, "worker-a");

            var otherCountry = new CountryRecord { Id = Guid.NewGuid(), Code = "test-country-b", Name = "País B", Iso2 = "FR", Iso3 = "FRA", IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now };
            var otherSource = new TerritorialDatasetSourceRecord
            {
                Id = Guid.NewGuid(), CountryId = otherCountry.Id, Organisation = "Official B", Dataset = "Pilot B", DatasetType = "AdministrativeTerritory", Locale = "fr-FR", Url = "https://example.test/b",
                License = "Open", Attribution = "Official B", CommercialUseAllowed = true, TransformationAllowed = true,
                ApprovalStatus = "Approved", VerifiedAtUtc = now, VerifiedByUserId = actor.Id, IsActive = true,
                PublicationMode = "FullSnapshot", DatasetVersion = "v1", CreatedAtUtc = now, UpdatedAtUtc = now
            };
            var otherType = new TerritorialUnitTypeRecord { Id = Guid.NewGuid(), CountryId = otherCountry.Id, Code = "MUNICIPALITY", Name = "Commune", DisplayOrder = 1, IsSelectableLocality = true, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now };
            var otherMapping = new TerritorialMappingTemplateRecord
            {
                Id = Guid.NewGuid(), DatasetSourceId = otherSource.Id, Version = 1, DefinitionJson = definitionJson,
                SchemaFingerprint = "schema", DefinitionChecksum = new string('b', 64), IsActive = true, CreatedAtUtc = now
            };
            db.AddRange(otherCountry, otherSource, otherType, otherMapping);
            await db.SaveChangesAsync();

            var sameCountryA = await service.QueueAsync(Request(source.Id, mapping.Id, actor.Id, "same-a", "multi-a"));
            var sameCountryB = await service.QueueAsync(Request(source.Id, mapping.Id, actor.Id, "same-b", "multi-b"));
            var otherCountryId = await service.QueueAsync(Request(otherSource.Id, otherMapping.Id, actor.Id, "other-country", "multi-c"));
            Assert.Equal(3, new[] { sameCountryA, sameCountryB, otherCountryId }.Distinct().Count());
            Assert.Equal(3, await db.TerritorialImportArtifacts.CountAsync(x => x.ImportId == sameCountryA || x.ImportId == sameCountryB || x.ImportId == otherCountryId));
            Assert.Equal("same-a", System.Text.Encoding.UTF8.GetString(await db.TerritorialImportArtifacts.Where(x => x.ImportId == sameCountryA).Select(x => x.Content).SingleAsync()));
            Assert.Equal("same-b", System.Text.Encoding.UTF8.GetString(await db.TerritorialImportArtifacts.Where(x => x.ImportId == sameCountryB).Select(x => x.Content).SingleAsync()));
            Assert.Equal("other-country", System.Text.Encoding.UTF8.GetString(await db.TerritorialImportArtifacts.Where(x => x.ImportId == otherCountryId).Select(x => x.Content).SingleAsync()));
            var queuedIds = new HashSet<Guid>();
            for (var index = 0; index < 3; index++)
            {
                var claimed = await queue.ClaimNextAsync("multi-worker", TimeSpan.FromMinutes(1)) ?? throw new InvalidOperationException();
                queuedIds.Add(claimed.ImportId);
                await queue.CancelClaimedAsync(claimed.ImportId, "multi-worker");
            }
            Assert.True(queuedIds.SetEquals([sameCountryA, sameCountryB, otherCountryId]));

            var retryId = await service.QueueAsync(Request(source.Id, mapping.Id, actor.Id, "retry", "retry"));
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var retryWork = await queue.ClaimNextAsync("retry-worker", TimeSpan.FromMinutes(1)) ?? throw new InvalidOperationException();
                Assert.Equal(retryId, retryWork.ImportId);
                await queue.FailAsync(retryId, "retry-worker", "WORKER_TRANSIENT", "Missatge funcional segur", true, 3);
                var retryState = await db.TerritorialImports.AsNoTracking().SingleAsync(x => x.Id == retryId);
                Assert.Equal(attempt < 3 ? "Queued" : "Failed", retryState.Status);
                Assert.Equal("WORKER_TRANSIENT", retryState.LastErrorCode);
                Assert.Equal("Missatge funcional segur", retryState.LastErrorMessage);
                Assert.Equal(attempt < 3, retryState.IsRecoverable);
                if (attempt < 3)
                    await db.TerritorialImports.Where(x => x.Id == retryId).ExecuteUpdateAsync(update => update.SetProperty(x => x.NextAttemptAtUtc, DateTimeOffset.UtcNow.AddSeconds(-1)));
            }

            var regionType = new TerritorialUnitTypeRecord { Id = Guid.NewGuid(), CountryId = country.Id, Code = "REGION", Name = "Regió", DisplayOrder = 2, IsSelectableLocality = false, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now };
            var provinceType = new TerritorialUnitTypeRecord { Id = Guid.NewGuid(), CountryId = country.Id, Code = "PROVINCE", Name = "Província", DisplayOrder = 3, IsSelectableLocality = false, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now };
            db.AddRange(regionType, provinceType);
            var originalSetId = await db.TerritorialChangeSets.Where(x => x.ImportId == firstId && x.RevertsChangeSetId == null).Select(x => x.Id).SingleAsync();
            var municipality = await db.TerritorialChangeSetItems.SingleAsync(x => x.ChangeSetId == originalSetId && x.CanonicalUnitKey == "unit:001");
            municipality.AfterJson = JsonSerializer.Serialize(new CanonicalTerritorialUnit(
                "unit:001", "province:01", "MUNICIPALITY", [new("Àgueda", "pt-PT", "Official")],
                [new("test:code", "001", true)], null, null), TerritorialImportJson.Options);
            var regionChangeId = Guid.NewGuid();
            var provinceChangeId = Guid.NewGuid();
            var secondMunicipalityId = Guid.NewGuid();
            db.TerritorialChangeSetItems.AddRange(
                new TerritorialChangeSetItemRecord
                {
                    Id = regionChangeId, ChangeSetId = originalSetId, Kind = "Create", CanonicalUnitKey = "region:01",
                    AfterJson = JsonSerializer.Serialize(new CanonicalTerritorialUnit(
                        "region:01", null, "REGION", [new("Regió Nord", "pt-PT", "Official")],
                        [new("test:region", "R1", true)], null, null), TerritorialImportJson.Options), ChangedFieldsJson = "[]"
                },
                new TerritorialChangeSetItemRecord
                {
                    Id = provinceChangeId, ChangeSetId = originalSetId, Kind = "Create", CanonicalUnitKey = "province:01",
                    AfterJson = JsonSerializer.Serialize(new CanonicalTerritorialUnit(
                        "province:01", "region:01", "PROVINCE", [new("Província Central", "pt-PT", "Official")],
                        [new("test:province", "P01", true)], null, null), TerritorialImportJson.Options), ChangedFieldsJson = "[]"
                },
                new TerritorialChangeSetItemRecord
                {
                    Id = secondMunicipalityId, ChangeSetId = originalSetId, Kind = "Update", CanonicalUnitKey = "unit:002",
                    AfterJson = JsonSerializer.Serialize(new CanonicalTerritorialUnit(
                        "unit:002", "province:01", "MUNICIPALITY", [new("Braga", "pt-PT", "Official")],
                        [new("test:code", "002", true)], null, null), TerritorialImportJson.Options),
                    ChangedFieldsJson = JsonSerializer.Serialize(new[] { "manualOverrideConflict:parent" }, TerritorialImportJson.Options)
                });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var regionHierarchy = await adminRepository.GetChangeHierarchyAsync(firstId, regionChangeId, new TerritorialChangeHierarchyQuery(null));
            Assert.NotNull(regionHierarchy);
            Assert.Empty(regionHierarchy.Ancestors);
            var provinceNode = Assert.Single(regionHierarchy.Children.Items);
            Assert.Equal("Província Central", provinceNode.Name);
            Assert.Equal("Província", provinceNode.TerritorialUnitType);
            Assert.Equal("Create", provinceNode.Kind);

            var provinceHierarchy = await adminRepository.GetChangeHierarchyAsync(firstId, provinceChangeId, new TerritorialChangeHierarchyQuery(null, 1, 1));
            Assert.NotNull(provinceHierarchy);
            Assert.Equal("Regió Nord", Assert.Single(provinceHierarchy.Ancestors).Name);
            Assert.Equal(2, provinceHierarchy.Children.TotalCount);
            Assert.Single(provinceHierarchy.Children.Items);
            Assert.Equal(2, provinceHierarchy.Children.TotalPages);
            var secondPage = await adminRepository.GetChangeHierarchyAsync(firstId, provinceChangeId, new TerritorialChangeHierarchyQuery(null, 2, 1));
            Assert.NotNull(secondPage);
            Assert.Single(secondPage.Children.Items);

            var byName = await adminRepository.GetChangeHierarchyAsync(firstId, provinceChangeId, new TerritorialChangeHierarchyQuery("Braga"));
            Assert.NotNull(byName);
            var conflictingChild = Assert.Single(byName.Children.Items);
            Assert.Equal(secondMunicipalityId, conflictingChild.ChangeId);
            Assert.True(conflictingChild.HasBlockingConflict);
            var byCode = await adminRepository.GetChangeHierarchyAsync(firstId, provinceChangeId, new TerritorialChangeHierarchyQuery("001"));
            Assert.NotNull(byCode);
            Assert.Equal("Àgueda", Assert.Single(byCode.Children.Items).Name);
            var leaf = await adminRepository.GetChangeHierarchyAsync(firstId, municipality.Id, new TerritorialChangeHierarchyQuery(null));
            Assert.NotNull(leaf);
            Assert.Equal(2, leaf.Ancestors.Count);
            Assert.Empty(leaf.Children.Items);

            var nonAdmin = new UserRecord { Id = Guid.NewGuid(), Email = $"territorial-user-{Guid.NewGuid():N}@example.test", Role = "User", PrivacyAccepted = true, PrivacyAcceptedAtUtc = now, CreatedAtUtc = now };
            db.Users.Add(nonAdmin);
            await db.SaveChangesAsync();
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.QueueAsync(Request(source.Id, mapping.Id, nonAdmin.Id, "forbidden", "v5")));
            Assert.False(await db.TerritorialImports.AnyAsync(x => x.CreatedByUserId == nonAdmin.Id));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    private static PrepareTerritorialImportRequest Request(Guid sourceId, Guid mappingId, Guid actorId, string content, string version = "v1") =>
        new(sourceId, mappingId, "pilot.xlsx", version, new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)), actorId);

    private static TerritorialDatasetSourceRecord Source(Guid countryId, Guid actorId, string key, string organisation, string approval, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), CountryId = countryId, SourceKey = key, Organisation = organisation, Dataset = "Territorial",
        DatasetType = "AdministrativeTerritory", Url = "https://example.test", ApprovalStatus = approval,
        VerifiedAtUtc = approval == "Approved" ? now : null, VerifiedByUserId = approval == "Approved" ? actorId : null,
        IsActive = true, PublicationMode = "FullSnapshot", DatasetVersion = "2026", DatasetDate = new DateOnly(2026, 1, 1),
        License = "Open data", LicenseUrl = "https://example.test/license", Attribution = organisation,
        CommercialUseAllowed = true, TransformationAllowed = true, CreatedAtUtc = now, UpdatedAtUtc = now
    };

    private static TerritorialSourceWorkbook NormalizedCoordinatePatch() => new([
        new("Manifest", [
            new(1, new Dictionary<string, string?> { ["A"] = "key", ["B"] = "value" }),
            new(2, new Dictionary<string, string?> { ["A"] = "contract_id", ["B"] = "PETILOC_TERRITORIAL" }),
            new(3, new Dictionary<string, string?> { ["A"] = "contract_version", ["B"] = "1.0" }),
            new(4, new Dictionary<string, string?> { ["A"] = "country_iso2", ["B"] = "ES" }),
            new(5, new Dictionary<string, string?> { ["A"] = "import_mode", ["B"] = "PATCH" }),
            new(6, new Dictionary<string, string?> { ["A"] = "dataset_version", ["B"] = "2026-coordinates" }),
            new(7, new Dictionary<string, string?> { ["A"] = "territorial_date", ["B"] = "2026-01-01" }),
            new(8, new Dictionary<string, string?> { ["A"] = "primary_source_key", ["B"] = "CNIG_NGMEP_2026" }),
            new(9, new Dictionary<string, string?> { ["A"] = "field_scope", ["B"] = "coordinates" }),
            new(10, new Dictionary<string, string?> { ["A"] = "coverage", ["B"] = "COUNTRY" })]),
        new("Fonts", [
            new(1, Headers(["source_key", "organisation", "dataset", "dataset_version", "territorial_date", "url", "license", "license_url", "attribution"])),
            new(2, new Dictionary<string, string?> { ["A"] = "ES-INE-30247", ["B"] = "INE", ["C"] = "Territorial" }),
            new(3, new Dictionary<string, string?> { ["A"] = "CNIG_NGMEP_2026", ["B"] = "CNIG", ["C"] = "Coordinates" })]),
        new("Unitats", [
            new(1, Headers(["unit_key", "identity_scheme", "identity_value", "type_code", "parent_unit_key", "primary_name", "primary_locale", "is_active", "structure_source_key", "latitude", "longitude", "coordinate_source_key"])),
            new(2, new Dictionary<string, string?> { ["A"] = "es:municipality:08006", ["B"] = "INE", ["C"] = "08006", ["J"] = "41.5812", ["K"] = "2.5503", ["L"] = "CNIG_NGMEP_2026" })])
    ], "normalized");

    private static Dictionary<string, string?> Headers(IReadOnlyList<string> values) => values
        .Select((value, index) => new KeyValuePair<string, string?>(((char)('A' + index)).ToString(), value)).ToDictionary();

    private static async Task<Guid> Prepare(TerritorialImportService service, ITerritorialImportWorkQueue queue, PrepareTerritorialImportRequest request)
    {
        var id = await service.QueueAsync(request);
        var work = await queue.ClaimNextAsync("test", TimeSpan.FromMinutes(1)) ?? throw new InvalidOperationException();
        Assert.Equal(id, work.ImportId);
        await service.ProcessPreparationAsync(work);
        await queue.CompleteAsync(id, "test");
        return id;
    }

    private static async Task Publish(TerritorialImportService service, ITerritorialImportWorkQueue queue, Guid id, Guid actorId)
    {
        await service.PublishAsync(id, actorId);
        var work = await queue.ClaimNextAsync("test", TimeSpan.FromMinutes(1)) ?? throw new InvalidOperationException();
        Assert.Equal(id, work.ImportId);
        await service.ProcessPublicationAsync(work);
        await queue.CompleteAsync(id, "test");
    }

    private static Task<string> Status(ZuppetoDbContext db, Guid id) =>
        db.TerritorialImports.AsNoTracking().Where(x => x.Id == id).Select(x => x.Status).SingleAsync();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Zuppeto.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("No s'ha trobat el repositori.");
    }

    private sealed class FixedWorkbookReader(TerritorialSourceWorkbook workbook) : ITerritorialWorkbookReader
    {
        public Task<TerritorialSourceWorkbook> ReadAsync(Stream source, CancellationToken cancellationToken = default) => Task.FromResult(workbook);
    }
}
