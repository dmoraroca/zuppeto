using Zuppeto.Application.TerritorialImports;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class TerritorialFunctionalTypeCatalogTests
{
    [Fact]
    public void Municipality_is_common_and_independent_cities_keep_both_functions()
    {
        Assert.True(TerritorialFunctionalTypeCatalog.TryOfficialTypeCodes("MUNICIPALITY", out var municipalities));
        Assert.Contains("MUNICIPALITY", municipalities);
        Assert.Contains("GEMEINDE", municipalities);
        Assert.Contains("KREISFREIE_STADT", municipalities);

        Assert.True(TerritorialFunctionalTypeCatalog.TryOfficialTypeCodes("PROVINCE_OR_DISTRICT", out var districts));
        Assert.Contains("KREISFREIE_STADT", districts);
        Assert.Equal("Municipi", TerritorialFunctionalTypeCatalog.CatalanTypeLabel("GEMEINDE", "Gemeinde"));
        Assert.Equal("Districte", TerritorialFunctionalTypeCatalog.CatalanTypeLabel("KREIS", "Kreis"));
    }

    [Fact]
    public void Context_categories_report_only_countries_with_compatible_official_types()
    {
        var spain = Guid.NewGuid();
        var germany = Guid.NewGuid();
        var categories = TerritorialFunctionalTypeCatalog.AvailableFor([
            new(Guid.NewGuid(), spain, "MUNICIPALITY", "Municipio", 10, true),
            new(Guid.NewGuid(), germany, "GEMEINDE", "Gemeinde", 10, true),
            new(Guid.NewGuid(), germany, "REGION", "Region", 20, false)
        ]);

        var municipality = Assert.Single(categories, item => item.Code == "MUNICIPALITY");
        Assert.Equal([spain, germany], municipality.CountryIds);
        Assert.Equal([germany], Assert.Single(categories, item => item.Code == "PLANNING_REGION").CountryIds);
        Assert.False(TerritorialFunctionalTypeCatalog.TryOfficialTypeCodes("UNKNOWN", out _));
    }
}
