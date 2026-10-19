using Zuppeto.Infrastructure.Places.MunicipalOpenData;

namespace Zuppeto.Infrastructure.Places.PublicAdministration;

internal static class AragonCommercialCategoryMapper
{
    private static readonly (string Prefix, string Category)[] IaeRules =
    [
        ("644", "bakery"),
        ("647", "shop"),
        ("651", "shop"),
        ("6521", "pharmacy"),
        ("652", "shop"),
        ("653", "shop"),
        ("654", "shop"),
        ("655", "shop"),
        ("659", "shop"),
        ("671", "restaurant"),
        ("672", "cafe"),
        ("673", "bar"),
        ("674", "restaurant"),
        ("681", "hotel"),
        ("682", "hostel"),
        ("687", "camping")
    ];

    public static bool TryMap(string? iaeCode, string? description,
        IReadOnlySet<string> requested, out string category)
    {
        var normalizedCode = new string((iaeCode ?? string.Empty).Where(char.IsDigit).ToArray());
        foreach (var rule in IaeRules)
        {
            if (!normalizedCode.StartsWith(rule.Prefix, StringComparison.Ordinal)
                || !requested.Contains(rule.Category)) continue;
            category = rule.Category;
            return true;
        }

        return DibaMunicipalCategoryMapper.TryMap(description, null, requested, out category);
    }
}
