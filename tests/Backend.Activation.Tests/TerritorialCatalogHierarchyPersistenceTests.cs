using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuppeto.Application.TerritorialImports;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;
using Zuppeto.Infrastructure.TerritorialImports;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class TerritorialCatalogHierarchyPersistenceTests
{
    [Fact]
    public async Task Published_catalog_supports_lazy_hierarchy_and_typed_paginated_descendants()
    {
        var connectionString = Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        var testConnection = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "zuppeto_territorial_phase7_hierarchy_tests"
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(testConnection).Options;
        await using var db = new ZuppetoDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        try
        {
            var now = DateTimeOffset.UtcNow;
            var es = Country("ES", "Espanya", now);
            var de = Country("DE", "Alemanya", now);
            db.Countries.AddRange(es, de);
            var community = Type(es.Id, "AUTONOMOUS_COMMUNITY", "Comunitat autònoma", 10, false, now);
            var province = Type(es.Id, "PROVINCE", "Província", 20, false, now);
            var municipality = Type(es.Id, "MUNICIPALITY", "Municipi", 30, true, now);
            var land = Type(de.Id, "BUNDESLAND", "Land", 10, false, now);
            var district = Type(de.Id, "KREIS", "Kreis", 20, false, now);
            var association = Type(de.Id, "GEMEINDEVERBAND", "Gemeindeverband", 30, false, now);
            var gemeinde = Type(de.Id, "GEMEINDE", "Gemeinde", 40, true, now);
            db.TerritorialUnitTypes.AddRange(community, province, municipality, land, district, association, gemeinde);

            var catalonia = Unit(es.Id, community.Id, null, now);
            var barcelona = Unit(es.Id, province.Id, catalonia.Id, now);
            var girona = Unit(es.Id, province.Id, catalonia.Id, now);
            var lleida = Unit(es.Id, province.Id, catalonia.Id, now);
            var tarragona = Unit(es.Id, province.Id, catalonia.Id, now);
            var arenys = Unit(es.Id, municipality.Id, barcelona.Id, now);
            var abrera = Unit(es.Id, municipality.Id, barcelona.Id, now);
            var landNode = Unit(de.Id, land.Id, null, now);
            var districtNode = Unit(de.Id, district.Id, landNode.Id, now);
            var associationNode = Unit(de.Id, association.Id, districtNode.Id, now);
            var gemeindeNode = Unit(de.Id, gemeinde.Id, associationNode.Id, now);
            db.TerritorialUnits.AddRange(catalonia, barcelona, girona, lleida, tarragona, arenys, abrera,
                landNode, districtNode, associationNode, gemeindeNode);
            db.TerritorialUnitNames.AddRange(
                Name(catalonia.Id, "Cataluña", "es-ES", now), Name(barcelona.Id, "Barcelona", "es-ES", now),
                Name(girona.Id, "Girona", "es-ES", now), Name(lleida.Id, "Lleida", "es-ES", now),
                Name(tarragona.Id, "Tarragona", "es-ES", now), Name(arenys.Id, "Arenys de Mar", "es-ES", now),
                Name(abrera.Id, "Abrera", "es-ES", now), Name(landNode.Id, "Schleswig-Holstein", "de-DE", now),
                Name(districtNode.Id, "Ostholstein", "de-DE", now),
                Name(associationNode.Id, "Neustadt in Holstein", "de-DE", now),
                Name(gemeindeNode.Id, "Neustadt in Holstein, Stadt", "de-DE", now));
            db.TerritorialUnitCodes.AddRange(
                Code(catalonia.Id, "09", now), Code(barcelona.Id, "08", now), Code(girona.Id, "17", now),
                Code(lleida.Id, "25", now), Code(tarragona.Id, "43", now), Code(arenys.Id, "08006", now),
                Code(abrera.Id, "08001", now), Code(gemeindeNode.Id, "01055032", now));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var repository = new TerritorialAdminRepository(db);
            var hierarchy = await repository.GetCatalogHierarchyAsync(catalonia.Id,
                new TerritorialCatalogHierarchyQuery(null, 1, 10));
            Assert.NotNull(hierarchy);
            Assert.Equal("Cataluña", hierarchy.Current.Name);
            Assert.Empty(hierarchy.Ancestors);
            Assert.Equal(4, hierarchy.Children.TotalCount);
            Assert.Equal(["Barcelona", "Girona", "Lleida", "Tarragona"], hierarchy.Children.Items.Select(x => x.Name));
            Assert.Equal(2, Assert.Single(hierarchy.DescendantTypes, x => x.TypeCode == "MUNICIPALITY").Count);

            var barcelonaHierarchy = await repository.GetCatalogHierarchyAsync(barcelona.Id,
                new TerritorialCatalogHierarchyQuery("Arenys", 1, 10));
            Assert.NotNull(barcelonaHierarchy);
            Assert.Equal("Cataluña", Assert.Single(barcelonaHierarchy.Ancestors).Name);
            Assert.Equal("Arenys de Mar", Assert.Single(barcelonaHierarchy.Children.Items).Name);

            var firstPage = await repository.ListCatalogDescendantsAsync(catalonia.Id,
                new TerritorialCatalogDescendantQuery(municipality.Id, null, 1, 1));
            Assert.Equal(2, firstPage.TotalCount);
            Assert.Equal(2, firstPage.TotalPages);
            Assert.Single(firstPage.Items);
            var byCode = await repository.ListCatalogDescendantsAsync(catalonia.Id,
                new TerritorialCatalogDescendantQuery(municipality.Id, "08006", 1, 10));
            var arenysResult = Assert.Single(byCode.Items);
            Assert.Equal("Arenys de Mar", arenysResult.PrimaryName);
            Assert.Equal("Barcelona", arenysResult.Parent);

            var leaf = await repository.GetCatalogHierarchyAsync(arenys.Id, new TerritorialCatalogHierarchyQuery(null));
            Assert.NotNull(leaf);
            Assert.Equal(2, leaf.Ancestors.Count);
            Assert.Empty(leaf.Children.Items);

            var deepGerman = await repository.GetCatalogHierarchyAsync(gemeindeNode.Id,
                new TerritorialCatalogHierarchyQuery(null));
            Assert.NotNull(deepGerman);
            Assert.Equal(3, deepGerman.Ancestors.Count);
            Assert.Equal(["Schleswig-Holstein", "Ostholstein", "Neustadt in Holstein"],
                deepGerman.Ancestors.Select(x => x.Name));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    private static CountryRecord Country(string code, string name, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), Code = code, Name = name, Iso2 = code, Iso3 = code + "X", IsActive = true,
        CreatedAtUtc = now, UpdatedAtUtc = now
    };
    private static TerritorialUnitTypeRecord Type(Guid countryId, string code, string name, int order, bool selectable, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), CountryId = countryId, Code = code, Name = name, DisplayOrder = order,
        IsSelectableLocality = selectable, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now
    };
    private static TerritorialUnitRecord Unit(Guid countryId, Guid typeId, Guid? parentId, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), CountryId = countryId, TerritorialUnitTypeId = typeId, ParentId = parentId,
        IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now
    };
    private static TerritorialUnitNameRecord Name(Guid unitId, string name, string locale, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), TerritorialUnitId = unitId, Name = name, NormalizedName = name.ToLowerInvariant(),
        Locale = locale, Kind = "Official", IsPrimary = true, CreatedAtUtc = now
    };
    private static TerritorialUnitCodeRecord Code(Guid unitId, string value, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), TerritorialUnitId = unitId, Scheme = "test:code", Value = value,
        IsPrimary = true, CreatedAtUtc = now
    };
}
