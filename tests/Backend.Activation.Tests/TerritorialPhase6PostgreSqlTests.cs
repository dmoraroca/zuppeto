using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuppeto.Application.TerritorialImports;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;
using Zuppeto.Infrastructure.TerritorialImports;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class TerritorialPhase6PostgreSqlTests
{
    [Fact]
    public async Task Catalog_maintenance_location_and_transitional_references_work_on_real_postgresql()
    {
        var connectionString = Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var testConnection = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "zuppeto_territorial_phase6_tests"
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(testConnection).Options;
        await using var db = new ZuppetoDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var spain = Country("ES", "España", "ES", "ESP", now);
            var germany = Country("DE", "Deutschland", "DE", "DEU", now);
            var provinceType = Type(spain.Id, "PROVINCE", "Província", false, 1, now);
            var municipalityType = Type(spain.Id, "MUNICIPALITY", "Municipi", true, 2, now);
            var germanMunicipalityType = Type(germany.Id, "MUNICIPALITY", "Gemeinde", true, 1, now);
            var province = Unit(spain.Id, provinceType.Id, null, true, now);
            var avila = Unit(spain.Id, municipalityType.Id, province.Id, true, now);
            var inactive = Unit(spain.Id, municipalityType.Id, province.Id, false, now);
            var notSelectable = Unit(spain.Id, provinceType.Id, null, true, now);
            var munich = Unit(germany.Id, germanMunicipalityType.Id, null, true, now);
            Name(province, "Àvila", "ca-ES", now);
            Name(avila, "Ávila", "es-ES", now);
            Name(inactive, "Àgueda", "ca-ES", now);
            Name(notSelectable, "Bayern", "de-DE", now);
            Name(munich, "München", "de-DE", now);
            Code(avila, "INE", "05019", now);
            Code(munich, "AGS", "DE09162", now);
            var actor = new UserRecord
            {
                Id = Guid.NewGuid(), Email = $"phase6-{Guid.NewGuid():N}@example.test", Role = "Admin",
                DisplayName = "Admin territorial", PrivacyAccepted = true, PrivacyAcceptedAtUtc = now, CreatedAtUtc = now
            };
            db.AddRange(spain, germany, provinceType, municipalityType, germanMunicipalityType,
                province, avila, inactive, notSelectable, munich, actor,
                new TerritorialCatalogStateRecord { CountryId = spain.Id, Version = 7, UpdatedAtUtc = now },
                new TerritorialCatalogStateRecord { CountryId = germany.Id, Version = 3, UpdatedAtUtc = now });
            await db.SaveChangesAsync();

            var catalog = new TerritorialAdminRepository(db);
            var page = await catalog.ListCatalogAsync(new(spain.Id, municipalityType.Id, "active", "es-ES", province.Id, "05019", true, 1, 1));
            Assert.Equal(1, page.TotalCount);
            Assert.Equal("Ávila", Assert.Single(page.Items).PrimaryName);
            Assert.Equal(province.Id, page.Items.Single().ParentId);
            var unicode = await catalog.ListCatalogAsync(new(germany.Id, null, "active", "de-DE", null, "München", true, 1, 50));
            Assert.Equal(munich.Id, Assert.Single(unicode.Items).Id);

            var locations = new TerritorialLocationRepository(db);
            var resolved = await locations.ResolveSelectionAsync(germany.Id, munich.Id);
            Assert.Equal("Deutschland", resolved.Country);
            Assert.Equal("München", resolved.Locality);
            await Assert.ThrowsAsync<InvalidOperationException>(() => locations.ResolveSelectionAsync(spain.Id, munich.Id));
            await Assert.ThrowsAsync<InvalidOperationException>(() => locations.ResolveSelectionAsync(spain.Id, inactive.Id));
            await Assert.ThrowsAsync<InvalidOperationException>(() => locations.ResolveSelectionAsync(spain.Id, notSelectable.Id));

            await catalog.MaintainAsync(avila.Id, actor.Id, new("deactivate", "Verificació fase VI"));
            await catalog.MaintainAsync(avila.Id, actor.Id, new("activate", "Verificació fase VI"));
            await catalog.MaintainAsync(avila.Id, actor.Id, new("set-selectable", "Verificació fase VI", false));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                catalog.MaintainAsync(avila.Id, actor.Id, new("set-coordinates", "Coordenades invàlides", Latitude: 0, Longitude: 0)));
            var detail = await catalog.MaintainAsync(avila.Id, actor.Id,
                new("set-coordinates", "Coordenades oficialment corregides", Latitude: 40.6565m, Longitude: -4.6810m));
            Assert.Equal(4, detail.Audit.Count);
            Assert.All(detail.Audit, item => Assert.False(string.IsNullOrWhiteSpace(item.BeforeValue)));
            Assert.All(detail.Audit, item => Assert.False(string.IsNullOrWhiteSpace(item.AfterValue)));
            Assert.Equal(11, await db.TerritorialCatalogStates.Where(x => x.CountryId == spain.Id).Select(x => x.Version).SingleAsync());

            var historicalUser = new UserRecord
            {
                Id = Guid.NewGuid(), Email = $"historic-{Guid.NewGuid():N}@example.test", Role = "User",
                City = "Text antic", Country = "País antic", PrivacyAccepted = true, PrivacyAcceptedAtUtc = now, CreatedAtUtc = now
            };
            var territorialUser = new UserRecord
            {
                Id = Guid.NewGuid(), Email = $"territorial-{Guid.NewGuid():N}@example.test", Role = "User",
                City = resolved.Locality, Country = resolved.Country, TerritorialCountryId = germany.Id,
                TerritorialUnitId = munich.Id, PrivacyAccepted = true, PrivacyAcceptedAtUtc = now, CreatedAtUtc = now
            };
            var historicalPlace = Place("Lloc històric", "Text antic", "País antic", null, null, now);
            var territorialPlace = Place("Lloc territorial", resolved.Locality, resolved.Country, germany.Id, munich.Id, now);
            db.AddRange(historicalUser, territorialUser, historicalPlace, territorialPlace);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Assert.Null((await db.Users.SingleAsync(x => x.Id == historicalUser.Id)).TerritorialUnitId);
            var persistedUser = await db.Users.SingleAsync(x => x.Id == territorialUser.Id);
            Assert.Equal(munich.Id, persistedUser.TerritorialUnitId);
            Assert.Equal("München", persistedUser.City);
            Assert.Null((await db.Places.SingleAsync(x => x.Id == historicalPlace.Id)).TerritorialUnitId);
            var persistedPlace = await db.Places.SingleAsync(x => x.Id == territorialPlace.Id);
            Assert.Equal(munich.Id, persistedPlace.TerritorialUnitId);
            Assert.Equal("Deutschland", persistedPlace.Country);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static CountryRecord Country(string code, string name, string iso2, string iso3, DateTimeOffset now) =>
        new() { Id = Guid.NewGuid(), Code = code, Name = name, Iso2 = iso2, Iso3 = iso3, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now };

    private static TerritorialUnitTypeRecord Type(Guid countryId, string code, string name, bool selectable, int order, DateTimeOffset now) =>
        new() { Id = Guid.NewGuid(), CountryId = countryId, Code = code, Name = name, IsSelectableLocality = selectable, DisplayOrder = order, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now };

    private static TerritorialUnitRecord Unit(Guid countryId, Guid typeId, Guid? parentId, bool active, DateTimeOffset now) =>
        new() { Id = Guid.NewGuid(), CountryId = countryId, TerritorialUnitTypeId = typeId, ParentId = parentId, IsActive = active, CreatedAtUtc = now, UpdatedAtUtc = now };

    private static void Name(TerritorialUnitRecord unit, string name, string locale, DateTimeOffset now) =>
        unit.Names.Add(new() { Id = Guid.NewGuid(), Name = name, NormalizedName = name.ToUpperInvariant(), Locale = locale, Kind = "Official", IsPrimary = true, CreatedAtUtc = now });

    private static void Code(TerritorialUnitRecord unit, string scheme, string value, DateTimeOffset now) =>
        unit.Codes.Add(new() { Id = Guid.NewGuid(), Scheme = scheme, Value = value, IsPrimary = true, CreatedAtUtc = now });

    private static PlaceRecord Place(string name, string city, string country, Guid? countryId, Guid? unitId, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(), Name = name, Type = "Cafe", ShortDescription = "Prova", Description = "Prova",
            CoverImageUrl = string.Empty, AddressLine1 = "Carrer de prova", City = city, Country = country,
            TerritorialCountryId = countryId, TerritorialUnitId = unitId, Latitude = 41, Longitude = 2,
            AcceptsDogs = true,
            PetPolicyLabel = "Admesos", PetPolicyNotes = string.Empty, PricingLabel = "€", DataProvenance = "Internal",
            CreatedAtUtc = now, UpdatedAtUtc = now
        };
}
