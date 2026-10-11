using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.TerritorialImports;

internal static class TerritorialCountryQuery
{
    public static IQueryable<CountryRecord> WithTerritorialCatalog(this IQueryable<CountryRecord> query) =>
        query.Where(country => country.IsActive && country.Iso2 != null && country.Iso2.Length == 2 &&
            (country.TerritorialUnitTypes.Any() || country.TerritorialDatasetSources.Any() || country.TerritorialUnits.Any()));
}
