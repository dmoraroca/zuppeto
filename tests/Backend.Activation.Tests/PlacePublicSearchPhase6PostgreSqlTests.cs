using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuppeto.Application.Places;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;
using Zuppeto.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Backend.Activation.Tests;

[Collection("PostgreSqlSequential")]
public sealed class PlacePublicSearchPhase6PostgreSqlTests
{
    private const string TemporaryDatabase = "zuppeto_place_search_phase6_tests";

    [Fact]
    public async Task PostgreSql_filters_radius_orders_and_pages_without_loading_the_full_catalog()
    {
        var baseConnection = Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");
        if (string.IsNullOrWhiteSpace(baseConnection)) return;

        var adminConnection = new NpgsqlConnectionStringBuilder(baseConnection) { Database = "postgres" }.ConnectionString;
        var testConnection = new NpgsqlConnectionStringBuilder(baseConnection) { Database = TemporaryDatabase }.ConnectionString;
        await RecreateDatabaseAsync(adminConnection);
        try
        {
            var options = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(testConnection).Options;
            await using var db = new ZuppetoDbContext(options);
            await db.Database.MigrateAsync();
            var now = DateTimeOffset.UtcNow;
            var proper = Place("Proper", "Restaurant", "Unknown", 41.581m, 2.550m, now.AddMinutes(-3));
            var distant = Place("Llunyà", "Restaurant", "Allowed", 41.620m, 2.590m, now.AddMinutes(-2));
            var outside = Place("Fora", "Hotel", "Unknown", 42.500m, 3.000m, now.AddMinutes(-1));
            var otherSpecies = Place("Espai per a conills", "Service", "Allowed", 41.582m, 2.552m, now);
            proper.AcceptsDogs = true;
            distant.AcceptsCats = true;
            otherSpecies.PetSpeciesJson = "[\"rabbit\"]";
            db.Places.AddRange(proper, distant, outside, otherSpecies);
            AddImportedCategory(db, proper, "pub", now);
            await db.SaveChangesAsync();

            var query = new PlacePublicSearchQuery(db);
            var result = await query.SearchAsync(new PlaceSearchRequest(
                null, null, null, null, "All", 0, 1,
                Types: ["Restaurant"],
                PetAccessStatuses: ["Unknown", "Allowed"],
                Sort: "Distance",
                RadiusKm: 10,
                CenterLatitude: 41.580m,
                CenterLongitude: 2.550m,
                North: 41.70m,
                South: 41.50m,
                East: 2.70m,
                West: 2.40m));

            Assert.Equal(2, result.Total);
            var item = Assert.Single(result.Items);
            Assert.Equal("Proper", item.Place.Name);
            Assert.NotNull(item.DistanceKm);
            Assert.True(item.DistanceKm < 1m);

            var importedCategory = await query.SearchAsync(new PlaceSearchRequest(
                null, null, "Arenys de Mar", null, "All", 0, 20,
                Types: ["pub"]));

            Assert.Equal(1, importedCategory.Total);
            Assert.Equal("Proper", Assert.Single(importedCategory.Items).Place.Name);

            var otherSpeciesResult = await query.SearchAsync(new PlaceSearchRequest(
                null, null, "Arenys de Mar", null, "Others", 0, 20));

            Assert.Equal(1, otherSpeciesResult.Total);
            Assert.Equal("Espai per a conills", Assert.Single(otherSpeciesResult.Items).Place.Name);

            var dogsOrCats = await query.SearchAsync(new PlaceSearchRequest(
                null, null, "Arenys de Mar", null, "Dogs,Cats", 0, 20));

            Assert.Equal(2, dogsOrCats.Total);
            Assert.Contains(dogsOrCats.Items, item => item.Place.Name == "Proper");
            Assert.Contains(dogsOrCats.Items, item => item.Place.Name == "Llunyà");
        }
        finally
        {
            await DropDatabaseAsync(adminConnection);
        }
    }

    private static PlaceRecord Place(string name, string type, string petStatus, decimal latitude, decimal longitude, DateTimeOffset updatedAt) =>
        new()
        {
            Id = Guid.NewGuid(), Name = name, Type = type, ShortDescription = "", Description = "",
            CoverImageUrl = "", AddressLine1 = "Carrer de la Riera 1", City = "Arenys de Mar", Country = "Espanya",
            Latitude = latitude, Longitude = longitude, PetPolicyStatus = petStatus,
            PetSpeciesJson = "[]", PetConditionsJson = "[]", PetPolicyLabel = "", PricingLabel = "",
            DataProvenance = "OpenStreetMap", CreatedAtUtc = updatedAt, UpdatedAtUtc = updatedAt
        };

    private static void AddImportedCategory(
        ZuppetoDbContext db,
        PlaceRecord place,
        string category,
        DateTimeOffset now)
    {
        var actorId = Guid.NewGuid();
        var countryId = Guid.NewGuid();
        var unitTypeId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var sourceRecordId = Guid.NewGuid();

        db.Users.Add(new UserRecord
        {
            Id = actorId, Email = $"phase6-{actorId:N}@test.local", Role = "Admin", PrivacyAccepted = true,
            PrivacyAcceptedAtUtc = now, EmailActivatedAtUtc = now, CreatedAtUtc = now
        });
        db.Countries.Add(new CountryRecord
        {
            Id = countryId, Code = "ES", Iso2 = "ES", Iso3 = "ESP", Name = "Espanya", IsActive = true,
            CreatedAtUtc = now, UpdatedAtUtc = now
        });
        db.TerritorialUnitTypes.Add(new TerritorialUnitTypeRecord
        {
            Id = unitTypeId, CountryId = countryId, Code = "MUNICIPALITY", Name = "Municipi",
            DisplayOrder = 1, IsSelectableLocality = true, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now
        });
        db.TerritorialUnits.Add(new TerritorialUnitRecord
        {
            Id = unitId, CountryId = countryId, TerritorialUnitTypeId = unitTypeId, IsActive = true,
            CreatedAtUtc = now, UpdatedAtUtc = now
        });
        db.PlaceDatasetSources.Add(new PlaceDatasetSourceRecord
        {
            Id = sourceId, SourceKey = "openstreetmap", Organisation = "OpenStreetMap contributors",
            Dataset = "OpenStreetMap", DatasetVersion = "test", Coverage = "Arenys de Mar",
            Url = "https://www.openstreetmap.org", AttributionUrl = "https://www.openstreetmap.org/copyright",
            LicenseIdentifier = "ODbL-1.0", LicenseUrl = "https://opendatacommons.org/licenses/odbl/1-0/",
            Attribution = "© OpenStreetMap contributors", ReuseConditions = "ODbL",
            PermanentStorageAllowed = true, CommercialUseAllowed = true, OdblDerived = true,
            ApprovalStatus = "Approved", ReviewedAtUtc = now, CreatedAtUtc = now
        });
        db.PlaceImportRuns.Add(new PlaceImportRunRecord
        {
            Id = runId, SourceId = sourceId, CountryId = countryId, TerritorialUnitId = unitId,
            CountryIso2 = "ES", TerritorialName = "Arenys de Mar", OfficialCodeScheme = "es:ine:municipality",
            OfficialCode = "08006", CategoriesJson = $"[\"{category}\"]", AccessMode = "GeofabrikExtract",
            Status = "Published", Stage = "Published", CreatedByActorId = actorId,
            CreatedAtUtc = now, UpdatedAtUtc = now
        });
        db.PlaceSourceRecords.Add(new PlaceSourceRecord
        {
            Id = sourceRecordId, SourceId = sourceId, ExternalId = "node/phase6", NormalizedPayloadJson = "{}",
            ContentHash = "phase6", EvidenceUri = "https://www.openstreetmap.org/node/phase6",
            ObservedAtUtc = now, RetrievedAtUtc = now, FirstSeenAtUtc = now, LastSeenAtUtc = now,
            State = "Active", CanonicalPlaceId = place.Id
        });
        db.PlaceImportItems.Add(new PlaceImportItemRecord
        {
            Id = Guid.NewGuid(), ImportRunId = runId, SourceRecordId = sourceRecordId, PlaceId = place.Id,
            Name = place.Name, Category = category, Latitude = place.Latitude, Longitude = place.Longitude,
            NormalizedFieldsJson = "{}", MatchBand = "ConfirmedMatch", MatchSignalsJson = "[]",
            MatchContradictionsJson = "[]", Status = "Published", IdempotencyHash = "phase6-category"
        });
    }

    private static async Task RecreateDatabaseAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await new NpgsqlCommand($"DROP DATABASE IF EXISTS {TemporaryDatabase} WITH (FORCE)", connection).ExecuteNonQueryAsync();
        await new NpgsqlCommand($"CREATE DATABASE {TemporaryDatabase}", connection).ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await new NpgsqlCommand($"DROP DATABASE IF EXISTS {TemporaryDatabase} WITH (FORCE)", connection).ExecuteNonQueryAsync();
    }
}
