using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Places;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;
using Zuppeto.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class PetGovernancePostgreSqlTests
{
    private const string TemporaryDatabase = "zuppeto_pet_governance_tests";
    private const string PreviousMigration = "20261013145700_AddPetFriendlyGovernancePhase3";

    [Fact]
    public async Task Temporary_PostgreSql_migrates_round_trips_and_preserves_audited_governance()
    {
        var baseConnection = Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");
        if (string.IsNullOrWhiteSpace(baseConnection)) return;

        var adminConnection = new NpgsqlConnectionStringBuilder(baseConnection) { Database = "postgres" }.ConnectionString;
        var testConnection = new NpgsqlConnectionStringBuilder(baseConnection) { Database = TemporaryDatabase }.ConnectionString;
        await RecreateDatabaseAsync(adminConnection);
        try
        {
            var options = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(testConnection).Options;
            await using (var migrationDb = new ZuppetoDbContext(options))
            {
                await migrationDb.Database.MigrateAsync();
                var migrator = migrationDb.GetService<IMigrator>();
                await migrator.MigrateAsync(PreviousMigration);
                await migrator.MigrateAsync();
            }

            var placeId = Guid.NewGuid();
            var actorId = Guid.NewGuid();
            await using (var db = new ZuppetoDbContext(options))
            {
                var now = DateTimeOffset.UtcNow;
                db.Users.Add(new UserRecord
                {
                    Id = actorId, Email = $"governance-{Guid.NewGuid():N}@example.test", Role = "Admin",
                    PrivacyAccepted = true, PrivacyAcceptedAtUtc = now, CreatedAtUtc = now
                });
                db.Places.Add(new PlaceRecord
                {
                    Id = placeId, Name = "Local temporal de governança", Type = "restaurant",
                    ShortDescription = "Prova", Description = "Prova aïllada", CoverImageUrl = "",
                    AddressLine1 = "Carrer temporal 1", City = "Municipi temporal", Country = "País temporal",
                    TerritorialCountryId = null, TerritorialUnitId = null, Latitude = null, Longitude = null,
                    PetPolicyStatus = "Unknown", PetPolicyLabel = "Desconeguda", PricingLabel = "",
                    DataProvenance = "Internal", CreatedAtUtc = now, UpdatedAtUtc = now
                });
                await db.SaveChangesAsync();

                var service = new PetGovernanceApplicationService(new PetGovernanceRepository(db), new PetAssertionPrecedencePolicy());
                var manual = await service.AddAssertionAsync(placeId,
                    Assertion("Allowed", "petiloc:manual", "Manual"), actorId);
                await service.VerifyAssertionAsync(placeId, manual.Id,
                    new("OnSite", "https://evidence.test/manual", "Comprovació presencial"), actorId);
                _ = await service.AddAssertionAsync(placeId,
                    Assertion("NotAllowed", "external:test", "Automatic"), actorId);
                var chain = await service.CreateChainAsync(new("Cadena temporal"), actorId);
                await service.LinkPlaceAsync(placeId, new(chain.Id, true, "Aplicabilitat confirmada"), actorId);
                _ = await service.AddChainPolicyAsync(chain.Id,
                    new("ES", null, "Allowed", ["dog"], [], now.AddDays(-1), null, true,
                        "chain:test", "https://evidence.test/chain", "Política oficial"), actorId);
                _ = await service.AddLocalExceptionAsync(placeId,
                    new(chain.Id, "dog", "TerraceOnly", [], "https://evidence.test/exception", "Excepció local", null), actorId);
            }

            await using (var recoveredDb = new ZuppetoDbContext(options))
            {
                var service = new PetGovernanceApplicationService(new PetGovernanceRepository(recoveredDb), new PetAssertionPrecedencePolicy());
                var overview = await service.GetOverviewAsync(placeId);
                var effective = Assert.Single(overview.EffectivePolicies);
                Assert.Equal("TerraceOnly", effective.Status);
                Assert.True(effective.IsVerifiedManual);
                Assert.False(effective.IsInheritedFromChain);
                Assert.Contains(overview.Assertions, x => x.SourceKey == "petiloc:manual" && x.Confidence == "Confirmed");
                Assert.Contains(overview.Assertions, x => x.SourceKey == "external:test");
                Assert.Contains(overview.History, x => x.Action == "Verified");
                Assert.Contains(overview.History, x => x.SubjectType == "LocalChainException");
                Assert.Equal("https://evidence.test/chain", Assert.Single(await recoveredDb.ChainPetPolicies.ToArrayAsync()).EvidenceUri);
                var place = await recoveredDb.Places.SingleAsync(x => x.Id == placeId);
                Assert.Null(place.TerritorialCountryId);
                Assert.Null(place.TerritorialUnitId);
                Assert.Null(place.Latitude);
                Assert.Null(place.Longitude);
            }
        }
        finally
        {
            await DropDatabaseAsync(adminConnection);
        }
    }

    private static CreatePetAssertionRequest Assertion(string status, string source, string origin) =>
        new("dog", status, [], source, null, status, DateTimeOffset.UtcNow, null, origin, "Unverified");

    private static async Task RecreateDatabaseAsync(string adminConnection)
    {
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await new NpgsqlCommand($"DROP DATABASE IF EXISTS {TemporaryDatabase} WITH (FORCE)", connection).ExecuteNonQueryAsync();
        await new NpgsqlCommand($"CREATE DATABASE {TemporaryDatabase}", connection).ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync(string adminConnection)
    {
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await new NpgsqlCommand($"DROP DATABASE IF EXISTS {TemporaryDatabase} WITH (FORCE)", connection).ExecuteNonQueryAsync();
    }
}
