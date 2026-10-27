namespace Zuppeto.Application.Places;

/// <summary>
/// Resolves the public category label from governed feature labels and the
/// canonical PetiLoc category. It has no dependency on an external provider.
/// </summary>
internal static class PlaceFeatureCategoryResolver
{
    private static readonly HashSet<string> PetCategoryLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Botiga d'animals",
        "Veterinària",
        "Cura d'animals",
        "Parc per a gossos",
        "Pinso",
        "Accessoris",
        "Productes per a mascotes",
        "Atenció veterinària",
        "Servei per a mascotes",
        "Espai per passejar"
    };

    private static readonly HashSet<string> CategoryLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Botiga d'animals",
        "Veterinària",
        "Cura d'animals",
        "Parc per a gossos",
        "Parc",
        "Zoològic",
        "Restaurant",
        "Cafeteria",
        "Bar",
        "Per emportar",
        "Entrega a domicili",
        "Allotjament",
        "Hotel",
        "Botiga",
        "Botiga de roba",
        "Supermercat",
        "Fleca",
        "Local nocturn",
        "Spa",
        "Gimnàs",
        "Perruqueria",
        "Saló de bellesa"
    };

    internal static string Resolve(IEnumerable<string> features, string canonicalCategoryLabel)
    {
        foreach (var feature in features)
        {
            var value = feature.Trim();
            if (value.Length > 0 && PetCategoryLabels.Contains(value))
            {
                return value;
            }
        }

        foreach (var feature in features)
        {
            var value = feature.Trim();
            if (value.Length > 0 && CategoryLabels.Contains(value))
            {
                return value;
            }
        }

        return !string.IsNullOrWhiteSpace(canonicalCategoryLabel)
            && !canonicalCategoryLabel.Equals("Servei", StringComparison.OrdinalIgnoreCase)
                ? canonicalCategoryLabel.Trim()
                : string.Empty;
    }
}
