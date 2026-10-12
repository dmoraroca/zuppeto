using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ValueObjects;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Mappings;
using Zuppeto.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class PlacePhaseOnePostgreSqlTests
{
    private const string TemporaryDatabase = "zuppeto_places_phase1_tests";

    [Fact]
    public async Task Temporary_PostgreSql_characterizes_constraints_filters_snapshots_cascades_and_territorial_foreign_keys()
    {
        var baseConnection = Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var adminConnection = new NpgsqlConnectionStringBuilder(baseConnection) { Database = "postgres" }.ConnectionString;
        var testConnection = new NpgsqlConnectionStringBuilder(baseConnection) { Database = TemporaryDatabase }.ConnectionString;
        await RecreateDatabaseAsync(adminConnection);

        try
        {
            var options = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(testConnection).Options;
            await using var db = new ZuppetoDbContext(options);
            await db.Database.MigrateAsync();

            var valid = Enumerable.Range(1, 25)
                .Select(index => PlaceFixture($"Cafè {index:00}", acceptsDogs: true, acceptsCats: index % 2 == 0))
                .ToArray();
            db.Places.AddRange(valid.Select(PlacePersistenceMapper.ToRecord));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var repository = new PlaceRepository(db);
            var filtered = await repository.SearchAsync(
                new PlaceSearchCriteria("Cafè", null, "Barcelona", PlaceType.Restaurant, PetCategory.Cats));
            Assert.Equal(12, filtered.Count);

            var snapshots = new PlaceSearchQueryRepository(db);
            var key = new Zuppeto.Domain.Abstractions.IPlaceSearchQueryRepository.SearchSnapshotKey(
                "cafè", "Barcelona", "Restaurant", "Cats");
            var now = DateTimeOffset.UtcNow;
            await snapshots.SaveSnapshotAsync(key, filtered.Select(place => place.Id).ToArray(), now, TimeSpan.FromHours(12));
            Assert.Equal(filtered.Select(place => place.Id), await snapshots.TryGetFreshPlaceIdsAsync(key, now.AddMinutes(1)));

            await snapshots.SaveSnapshotAsync(key, [valid[0].Id], now, TimeSpan.FromMinutes(-1));
            Assert.Null(await snapshots.TryGetFreshPlaceIdsAsync(key, now));

            await AssertDatabaseConstraintAsync(db, PlaceFixture("Cap mascota", false, false));
            await AssertTerritorialForeignKeyAsync(db, PlaceFixture(
                "Referència territorial inexistent", true, false, Guid.NewGuid(), Guid.NewGuid()));

            await snapshots.SaveSnapshotAsync(key, [valid[1].Id], now, TimeSpan.FromHours(12));
            Assert.True(await repository.DeleteAsync(valid[1].Id));
            Assert.Equal(0, await db.PlaceSearchQueryResults.CountAsync(row => row.PlaceId == valid[1].Id));
            Assert.Equal(1, await db.PlaceSearchQueries.CountAsync(row => row.QueryKey == "cafè|barcelona|restaurant|cats"));
        }
        finally
        {
            await DropDatabaseAsync(adminConnection);
        }
    }

    private static async Task AssertDatabaseConstraintAsync(ZuppetoDbContext db, Place place)
    {
        db.Places.Add(PlacePersistenceMapper.ToRecord(place));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
    }

    private static async Task AssertTerritorialForeignKeyAsync(ZuppetoDbContext db, Place place)
    {
        db.Places.Add(PlacePersistenceMapper.ToRecord(place));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
    }

    private static Place PlaceFixture(
        string name,
        bool acceptsDogs,
        bool acceptsCats,
        Guid? countryId = null,
        Guid? unitId = null) =>
        new(
            Guid.NewGuid(), name, PlaceType.Restaurant, "Breu", "Descripció", "",
            new PostalAddress("Carrer Major 1", "Barcelona", "Espanya", ""),
            new GeoLocation(41.38m, 2.17m),
            new PetPolicy(acceptsDogs, acceptsCats, "Política", ""),
            new Pricing("€€"), new RatingSnapshot(4m, 10),
            territorialCountryId: countryId, territorialUnitId: unitId);

    private static async Task RecreateDatabaseAsync(string adminConnection)
    {
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {TemporaryDatabase} WITH (FORCE)", connection);
        await drop.ExecuteNonQueryAsync();
        await using var create = new NpgsqlCommand($"CREATE DATABASE {TemporaryDatabase}", connection);
        await create.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync(string adminConnection)
    {
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS {TemporaryDatabase} WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }
}
