namespace Zuppeto.Application.TerritorialImports;

public static class TerritorialFunctionalTypeCatalog
{
    private sealed record Definition(string Code, string Name, int DisplayOrder, string[] OfficialTypeCodes);

    private static readonly Definition[] Definitions =
    [
        new("MUNICIPALITY", "Municipi", 10,
            ["MUNICIPALITY", "AUTONOMOUS_CITY_MUNICIPALITY", "GEMEINDE", "KREISFREIE_STADT"]),
        new("FIRST_LEVEL_ADMINISTRATION", "Divisió administrativa principal", 20,
            ["AUTONOMOUS_COMMUNITY", "BUNDESLAND"]),
        new("PROVINCE_OR_DISTRICT", "Província o districte", 30,
            ["PROVINCE", "KREIS", "KREISFREIE_STADT"]),
        new("ADMINISTRATIVE_REGION", "Regió administrativa", 40, ["REGIERUNGSBEZIRK"]),
        new("STATISTICAL_REGION", "Regió estadística", 50, ["STATISTISCHE_REGION"]),
        new("PLANNING_REGION", "Regió de planificació", 60, ["REGION"]),
        new("MUNICIPAL_ASSOCIATION", "Agrupació municipal", 70, ["GEMEINDEVERBAND"]),
        new("SPECIAL_TERRITORY", "Territori especial", 80, ["SPECIAL_TERRITORY"])
    ];

    private static readonly IReadOnlyDictionary<string, string> CatalanTypeLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AUTONOMOUS_COMMUNITY"] = "Comunitat autònoma",
            ["AUTONOMOUS_CITY_MUNICIPALITY"] = "Ciutat autònoma",
            ["PROVINCE"] = "Província",
            ["MUNICIPALITY"] = "Municipi",
            ["BUNDESLAND"] = "Estat federat",
            ["REGIERUNGSBEZIRK"] = "Districte governamental",
            ["STATISTISCHE_REGION"] = "Regió estadística",
            ["REGION"] = "Regió de planificació",
            ["KREIS"] = "Districte",
            ["KREISFREIE_STADT"] = "Ciutat independent",
            ["GEMEINDEVERBAND"] = "Agrupació municipal",
            ["GEMEINDE"] = "Municipi",
            ["SPECIAL_TERRITORY"] = "Territori especial"
        };

    public static IReadOnlyCollection<TerritorialFunctionalTypeDto> AvailableFor(
        IReadOnlyCollection<TerritorialAdminUnitTypeDto> officialTypes) =>
        Definitions.Select(definition => new TerritorialFunctionalTypeDto(
                definition.Code,
                definition.Name,
                definition.DisplayOrder,
                officialTypes.Where(type => definition.OfficialTypeCodes.Contains(type.Code, StringComparer.OrdinalIgnoreCase))
                    .Select(type => type.CountryId).Distinct().ToArray()))
            .Where(type => type.CountryIds.Count > 0)
            .ToArray();

    public static bool TryOfficialTypeCodes(string? functionalTypeCode, out IReadOnlyCollection<string> officialTypeCodes)
    {
        var definition = Definitions.SingleOrDefault(item =>
            string.Equals(item.Code, functionalTypeCode?.Trim(), StringComparison.OrdinalIgnoreCase));
        officialTypeCodes = definition?.OfficialTypeCodes ?? [];
        return definition is not null;
    }

    public static string CatalanTypeLabel(string officialTypeCode, string fallback) =>
        CatalanTypeLabels.TryGetValue(officialTypeCode, out var label) ? label : fallback;
}
