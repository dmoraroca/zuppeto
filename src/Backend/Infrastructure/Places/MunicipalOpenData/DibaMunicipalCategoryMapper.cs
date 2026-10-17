using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Zuppeto.Infrastructure.Places.MunicipalOpenData;

internal static class DibaMunicipalCategoryMapper
{
    private static readonly (string Category, string[] Terms)[] Rules =
    [
        ("pet_grooming", ["perruqueria canina", "perruqueria d animals"]),
        ("pet_shop", ["botiga d animals", "animals de companyia"]),
        ("veterinary", ["veterinari", "clinica veterinaria"]),
        ("dog_park", ["parc de gossos", "area d esbarjo per a gossos"]),
        ("camping", ["camping", "campament"]),
        ("hostel", ["hostal", "pensio", "alberg"]),
        ("hotel", ["hotel"]),
        ("apartment", ["apartament turistic", "habitatge d us turistic"]),
        ("bakery", ["forn de pa", "venda de pa", "panaderia", "fleca", "pastisseria"]),
        ("fast_food", ["menjar rapid", "fast food", "hamburgueseria"]),
        ("restaurant", ["restaurant", "restauracio", "pizzeria"]),
        ("cafe", ["cafeteria", "granja"]),
        ("pub", ["pub"]),
        ("bar", ["bar", "taverna"]),
        ("pharmacy", ["farmacia"]),
        ("fuel", ["benzinera", "estacio de servei", "carburant"]),
        ("ice_cream", ["gelateria"]),
        ("nightclub", ["discoteca", "sala de ball"]),
        ("park", ["parc public", "jardi public"]),
        ("shop", ["comerc al detall", "supermercat", "botiga", "venda"])
    ];

    public static bool TryMap(string? activity, string? otherActivities,
        IReadOnlySet<string> requested, out string category)
    {
        var text = Fold($"{activity} {otherActivities}");
        foreach (var rule in Rules)
        {
            if (!requested.Contains(rule.Category) || !rule.Terms.Any(term => ContainsTerm(text, term))) continue;
            category = rule.Category;
            return true;
        }
        category = string.Empty;
        return false;
    }

    private static bool ContainsTerm(string text, string term) => Regex.IsMatch(text,
        $"(?:^|[^a-z0-9]){Regex.Escape(term)}(?:$|[^a-z0-9])", RegexOptions.CultureInvariant);

    private static string Fold(string value)
    {
        var decomposed = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(decomposed.Where(character =>
            CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark).ToArray());
    }
}
