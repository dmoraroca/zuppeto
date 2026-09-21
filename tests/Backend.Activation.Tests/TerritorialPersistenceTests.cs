using Microsoft.EntityFrameworkCore;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class TerritorialPersistenceTests
{
    [Fact]
    public async Task PostgreSql_model_persists_variable_pilot_hierarchies_and_enforces_critical_constraints()
    {
        var connectionString = Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var options = new DbContextOptionsBuilder<ZuppetoDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var db = new ZuppetoDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        try
        {
            var now = DateTimeOffset.UtcNow;
            var spain = Country("ES-current", "Espanya", "ES", "ESP", now);
            var germany = Country("DE-current", "Alemanya", "DE", "DEU", now);
            db.Countries.AddRange(spain, germany);

            var community = Type(spain.Id, "AUTONOMOUS-COMMUNITY", "Comunitat autònoma", 10, false, now);
            var province = Type(spain.Id, "PROVINCE", "Província", 20, false, now);
            var municipality = Type(spain.Id, "MUNICIPALITY", "Municipi", 30, true, now);
            var autonomousCity = Type(spain.Id, "AUTONOMOUS-CITY-MUNICIPALITY", "Ciutat autònoma", 30, true, now);
            var land = Type(germany.Id, "BUNDESLAND", "Bundesland", 10, false, now);
            var rb = Type(germany.Id, "REGIERUNGSBEZIRK", "Regierungsbezirk", 20, false, now);
            var kreis = Type(germany.Id, "KREIS", "Kreis", 30, false, now);
            var association = Type(germany.Id, "GEMEINDEVERBAND", "Gemeindeverband", 40, false, now);
            var gemeinde = Type(germany.Id, "GEMEINDE", "Gemeinde", 50, true, now);
            var independentCity = Type(germany.Id, "KREISFREIE-STADT", "Kreisfreie Stadt", 30, true, now);
            db.TerritorialUnitTypes.AddRange(community, province, municipality, autonomousCity, land, rb, kreis, association, gemeinde, independentCity);

            var esSource = Source(spain.Id, "INE", "Relació de municipis", now);
            var deSource = Source(germany.Id, "Destatis", "GV-ISys", now);
            db.TerritorialDatasetSources.AddRange(esSource, deSource);

            var catalonia = Unit(spain.Id, community.Id, null, now);
            var barcelona = Unit(spain.Id, province.Id, catalonia.Id, now);
            var arenys = Unit(spain.Id, municipality.Id, barcelona.Id, now, 41.58m, 2.55m, esSource.Id);
            var ceuta = Unit(spain.Id, autonomousCity.Id, null, now);
            var bayern = Unit(germany.Id, land.Id, null, now);
            var munichDistrict = Unit(germany.Id, kreis.Id, bayern.Id, now);
            var munich = Unit(germany.Id, gemeinde.Id, munichDistrict.Id, now);
            var nrw = Unit(germany.Id, land.Id, null, now);
            var cologneRb = Unit(germany.Id, rb.Id, nrw.Id, now);
            var district = Unit(germany.Id, kreis.Id, cologneRb.Id, now);
            var verband = Unit(germany.Id, association.Id, district.Id, now);
            var gemeindeDeep = Unit(germany.Id, gemeinde.Id, verband.Id, now);
            var berlin = Unit(germany.Id, independentCity.Id, null, now);
            db.TerritorialUnits.AddRange(catalonia, barcelona, arenys, ceuta, bayern, munichDistrict, munich, nrw, cologneRb, district, verband, gemeindeDeep, berlin);

            db.TerritorialUnitNames.AddRange(
                Name(arenys.Id, "Arenys de Mar", "ca-ES", now),
                Name(ceuta.Id, "Ceuta", "es-ES", now),
                Name(munich.Id, "München", "de-DE", now),
                Name(gemeindeDeep.Id, "München", "de-DE", now),
                Name(berlin.Id, "Berlin", "de-DE", now));
            db.TerritorialUnitCodes.AddRange(
                Code(arenys.Id, "es:ine:municipality", "08006", now),
                Code(munich.Id, "de:destatis:ags", "09184149", now),
                Code(munich.Id, "de:destatis:ars", "091841490000", now));
            db.TerritorialLocaleAssignments.AddRange(
                Locale(spain.Id, null, "es-ES", 0, now),
                Locale(spain.Id, catalonia.Id, "ca-ES", 0, now),
                Locale(spain.Id, catalonia.Id, "es-ES", 1, now),
                Locale(germany.Id, null, "de-DE", 0, now));

            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Assert.Equal(13, await db.TerritorialUnits.CountAsync());
            Assert.Equal(2, await db.TerritorialUnitNames.CountAsync(item => item.Name == "München"));
            Assert.Equal("08006", await db.TerritorialUnitCodes.Where(item => item.Scheme == "es:ine:municipality").Select(item => item.Value).SingleAsync());
            Assert.Equal(5, await AncestorDepth(db, gemeindeDeep.Id));
            Assert.Equal(1, await AncestorDepth(db, ceuta.Id));

            db.TerritorialUnits.Add(Unit(spain.Id, municipality.Id, munich.Id, now));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();

            var incomplete = Unit(spain.Id, municipality.Id, null, now);
            incomplete.Latitude = 10;
            db.TerritorialUnits.Add(incomplete);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();

            db.TerritorialUnitTypes.Remove(await db.TerritorialUnitTypes.SingleAsync(item => item.Id == municipality.Id));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<int> AncestorDepth(ZuppetoDbContext db, Guid unitId)
    {
        var depth = 0;
        Guid? current = unitId;
        while (current is not null)
        {
            depth++;
            current = await db.TerritorialUnits.Where(item => item.Id == current).Select(item => item.ParentId).SingleAsync();
        }

        return depth;
    }

    private static CountryRecord Country(string code, string name, string iso2, string iso3, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), Code = code, Name = name, Iso2 = iso2, Iso3 = iso3,
        IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now
    };

    private static TerritorialUnitTypeRecord Type(Guid countryId, string code, string name, int order, bool selectable, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), CountryId = countryId, Code = code, Name = name, DisplayOrder = order,
        IsSelectableLocality = selectable, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now
    };

    private static TerritorialDatasetSourceRecord Source(Guid countryId, string organisation, string dataset, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), CountryId = countryId, Organisation = organisation, Dataset = dataset,
        Url = "https://example.test", ApprovalStatus = "Pending", PublicationMode = "FullSnapshot",
        IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now
    };

    private static TerritorialUnitRecord Unit(
        Guid countryId, Guid typeId, Guid? parentId, DateTimeOffset now,
        decimal? latitude = null, decimal? longitude = null, Guid? sourceId = null) => new()
    {
        Id = Guid.NewGuid(), CountryId = countryId, TerritorialUnitTypeId = typeId, ParentId = parentId,
        Latitude = latitude, Longitude = longitude, CoordinateSourceId = sourceId,
        IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now
    };

    private static TerritorialUnitNameRecord Name(Guid unitId, string name, string locale, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), TerritorialUnitId = unitId, Name = name, Locale = locale,
        NormalizedName = name.ToLowerInvariant(), Kind = "Official", IsPrimary = true, CreatedAtUtc = now
    };

    private static TerritorialUnitCodeRecord Code(Guid unitId, string scheme, string value, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), TerritorialUnitId = unitId, Scheme = scheme, Value = value,
        IsPrimary = true, CreatedAtUtc = now
    };

    private static TerritorialLocaleAssignmentRecord Locale(Guid countryId, Guid? unitId, string locale, int priority, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), CountryId = countryId, TerritorialUnitId = unitId, Locale = locale,
        IsOfficial = true, Priority = priority, CreatedAtUtc = now, UpdatedAtUtc = now
    };
}
