using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
    private const string PreviousCoverageMigration = "20261015144014_AddPlaceMediaAssetsPhase6";

    [Fact]
    public async Task Coverage_migration_round_trip_backfills_category_and_terrace_without_pet_inference()
    {
        var baseConnection = Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");
        if (string.IsNullOrWhiteSpace(baseConnection)) return;
        var adminConnection = new NpgsqlConnectionStringBuilder(baseConnection) { Database = "postgres" }.ConnectionString;
        var testConnection = new NpgsqlConnectionStringBuilder(baseConnection) { Database = TemporaryDatabase }.ConnectionString;
        await RecreateDatabaseAsync(adminConnection);
        try
        {
            var options = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(testConnection).Options;
            await using (var db = new ZuppetoDbContext(options))
            {
                await db.Database.MigrateAsync();
                var now = DateTimeOffset.UtcNow;
                var bakery = Place("Fleca temporal", "Bar", "Unknown", 41.58m, 2.55m, now);
                db.Places.Add(bakery);
                AddImportedCategory(db, bakery, "bakery", now, "{\"amenity.outdoor_seating\":\"Available\"}");
                await db.SaveChangesAsync();
            }
            await using (var down = new ZuppetoDbContext(options))
                await down.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().MigrateAsync(PreviousCoverageMigration);
            await using (var up = new ZuppetoDbContext(options))
                await up.Database.MigrateAsync();
            await using (var verify = new ZuppetoDbContext(options))
            {
                var place = await verify.Places.Include(item => item.PlaceFeatures).ThenInclude(link => link.Feature).SingleAsync();
                Assert.Equal("bakery", place.CommercialCategory);
                Assert.Contains(place.PlaceFeatures, link => link.Feature.Code == "Terrassa");
                Assert.Equal("Unknown", place.PetPolicyStatus);
                Assert.False(place.AcceptsDogs);
            }
            await using (var downAgain = new ZuppetoDbContext(options))
                await downAgain.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().MigrateAsync(PreviousCoverageMigration);
            await using (var upAgain = new ZuppetoDbContext(options))
                await upAgain.Database.MigrateAsync();
            await using (var idempotent = new ZuppetoDbContext(options))
                Assert.Equal(1, await idempotent.PlaceFeatures.CountAsync());
        }
        finally
        {
            await DropDatabaseAsync(adminConnection);
        }
    }

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
            Assert.Equal(2, result.MapPoints!.Count);
            Assert.Contains(result.MapPoints, point => point.Name == "Llunyà");

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

            var assertionId = Guid.NewGuid();
            db.PetFriendlyAssertions.Add(new PetFriendlyAssertionRecord
            {
                Id = assertionId, PlaceId = proper.Id, SpeciesCode = "dog", Status = "NotAllowed",
                ConditionsJson = "[]", SourceKey = "petiloc:manual", ObservedValue = "No admet gossos",
                ObservedAtUtc = now, VerifiedAtUtc = now, VerificationMethod = "OnSite", Confidence = "Confirmed",
                ResponsibleActorId = Guid.NewGuid(), EvidenceUri = "https://evidence.test/proper", Origin = "Manual", State = "Current"
            });
            await db.SaveChangesAsync();
            var governed = await query.SearchAsync(new PlaceSearchRequest(
                null, null, "Arenys de Mar", null, "All", 0, 20,
                PetAccessStatuses: ["NotAllowed"], VerifiedOnly: true));
            Assert.Equal("NotAllowed", Assert.Single(governed.Items).Place.PetPolicy.Status.ToString());
            Assert.True(Assert.Single(governed.Items).IsPetPolicyVerified);
            Assert.Equal("NotAllowed", Assert.Single(governed.MapPoints!).PetAccessStatus);

            var assertion = await db.PetFriendlyAssertions.SingleAsync(item => item.Id == assertionId);
            assertion.State = "Revoked";
            assertion.VerifiedAtUtc = null;
            await db.SaveChangesAsync();
            var revoked = await query.SearchAsync(new PlaceSearchRequest(
                null, null, "Arenys de Mar", null, "All", 0, 20,
                PetAccessStatuses: ["Unknown"], VerifiedOnly: false));
            Assert.Contains(revoked.Items, item => item.Place.Id == proper.Id && !item.IsPetPolicyVerified);
        }
        finally
        {
            await DropDatabaseAsync(adminConnection);
        }
    }

    [Fact]
    public async Task Arenys_map_returns_23_filtered_or_all_50_while_list_remains_paged()
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
            db.Places.AddRange(Enumerable.Range(0, 50).Select(index =>
            {
                var type = index < 23
                    ? (index % 3) switch { 0 => "Hotel", 1 => "Bar", _ => "Restaurant" }
                    : index % 3 == 1 ? "Park" : "Service";
                var commercialCategory = index < 23
                    ? type.ToLowerInvariant()
                    : (index % 3) switch { 0 => "pet_service", 1 => "park", _ => "shop" };
                return Place($"Local {index:00}", type, "Unknown",
                    41.56m + (index % 10) / 1000m, 2.53m + (index / 10) / 1000m, now,
                    commercialCategory);
            }));
            await db.SaveChangesAsync();
            var query = new PlacePublicSearchQuery(db);

            var filtered = await query.SearchAsync(new PlaceSearchRequest(
                null, null, "Arenys de Mar", null, "All", 0, 20,
                Types: ["hotel", "bar", "restaurant"]));
            Assert.Equal(23, filtered.Total);
            Assert.Equal(20, filtered.Items.Count);
            Assert.Equal(23, filtered.MapPoints!.Count);

            var all = await query.SearchAsync(new PlaceSearchRequest(
                null, null, "Arenys de Mar", null, "All", 0, 20));
            Assert.Equal(50, all.Total);
            Assert.Equal(20, all.Items.Count);
            Assert.Equal(50, all.MapPoints!.Count);
            Assert.Equal(50, all.MapPoints.Select(point => point.Id).Distinct().Count());

            var services = await query.SearchAsync(new PlaceSearchRequest(
                null, null, "Arenys de Mar", null, "All", 0, 20, Types: ["service"]));
            Assert.Equal(18, services.Total);
            var parks = await query.SearchAsync(new PlaceSearchRequest(
                null, null, "Arenys de Mar", null, "All", 0, 20, Types: ["park"]));
            Assert.Equal(9, parks.Total);
            var shops = await query.SearchAsync(new PlaceSearchRequest(
                null, null, "Arenys de Mar", null, "All", 0, 20, Types: ["shop"]));
            Assert.Equal(9, shops.Total);

            var unscoped = await query.SearchAsync(new PlaceSearchRequest(
                null, null, null, null, "All", 0, 20));
            Assert.Equal(50, unscoped.Total);
            Assert.Empty(unscoped.MapPoints!);
        }
        finally
        {
            await DropDatabaseAsync(adminConnection);
        }
    }

    [Theory]
    [InlineData(500)]
    [InlineData(5_000)]
    public async Task Map_returns_all_lightweight_points_at_scale_while_list_remains_paged(int placeCount)
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
            db.Places.AddRange(Enumerable.Range(0, placeCount).Select(index =>
                Place($"Local {index:0000}", "Restaurant", "Unknown",
                    41.56m + (index % 100) / 10000m, 2.53m + (index / 100) / 10000m, now)));
            await db.SaveChangesAsync();

            var result = await new PlacePublicSearchQuery(db).SearchAsync(new PlaceSearchRequest(
                null, null, "Arenys de Mar", null, "All", 0, 20));

            Assert.Equal(placeCount, result.Total);
            Assert.Equal(20, result.Items.Count);
            Assert.Equal(placeCount, result.MapPoints!.Count);
            Assert.Equal(placeCount, result.MapPoints.Select(point => point.Id).Distinct().Count());
        }
        finally
        {
            await DropDatabaseAsync(adminConnection);
        }
    }

    private static PlaceRecord Place(
        string name,
        string type,
        string petStatus,
        decimal latitude,
        decimal longitude,
        DateTimeOffset updatedAt,
        string? commercialCategory = null) =>
        new()
        {
            Id = Guid.NewGuid(), Name = name, Type = type, ShortDescription = "", Description = "",
            CoverImageUrl = "", AddressLine1 = "Carrer de la Riera 1", City = "Arenys de Mar", Country = "Espanya",
            Latitude = latitude, Longitude = longitude, PetPolicyStatus = petStatus,
            PetSpeciesJson = "[]", PetConditionsJson = "[]", PetPolicyLabel = "", PricingLabel = "",
            DataProvenance = "OpenStreetMap", CreatedAtUtc = updatedAt, UpdatedAtUtc = updatedAt,
            CommercialCategory = commercialCategory
        };

    private static void AddImportedCategory(
        ZuppetoDbContext db,
        PlaceRecord place,
        string category,
        DateTimeOffset now,
        string normalizedFieldsJson = "{}")
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
            NormalizedFieldsJson = normalizedFieldsJson, MatchBand = "ConfirmedMatch", MatchSignalsJson = "[]",
            MatchContradictionsJson = "[]", Status = "Published", IdempotencyHash = "phase6-category"
        });
    }

    private static async Task RecreateDatabaseAsync(string connectionString)
    {
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await new NpgsqlCommand($"DROP DATABASE IF EXISTS {TemporaryDatabase} WITH (FORCE)", connection).ExecuteNonQueryAsync();
        await new NpgsqlCommand($"CREATE DATABASE {TemporaryDatabase}", connection).ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync(string connectionString)
    {
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await new NpgsqlCommand($"DROP DATABASE IF EXISTS {TemporaryDatabase} WITH (FORCE)", connection).ExecuteNonQueryAsync();
        NpgsqlConnection.ClearAllPools();
    }
}
