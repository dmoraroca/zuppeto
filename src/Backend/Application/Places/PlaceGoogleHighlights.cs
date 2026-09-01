namespace Zuppeto.Application.Places;

internal static class PlaceGoogleHighlights
{
    private static readonly HashSet<string> SkippedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "establishment",
        "point_of_interest",
        "premise",
        "geocode",
        "political",
        "route",
        "street_address",
        "plus_code",
        "subpremise",
        "floor",
        "neighborhood",
        "locality",
        "postal_code",
        "country",
        "food"
    };

    private static readonly Dictionary<string, string> TypeLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pet_store"] = "Botiga d'animals",
        ["veterinary_care"] = "Veterinària",
        ["pet_care"] = "Cura d'animals",
        ["restaurant"] = "Restaurant",
        ["cafe"] = "Cafeteria",
        ["bar"] = "Bar",
        ["meal_takeaway"] = "Per emportar",
        ["meal_delivery"] = "Entrega a domicili",
        ["lodging"] = "Allotjament",
        ["hotel"] = "Hotel",
        ["park"] = "Parc",
        ["store"] = "Botiga",
        ["clothing_store"] = "Botiga de roba",
        ["grocery_or_supermarket"] = "Supermercat",
        ["supermarket"] = "Supermercat",
        ["bakery"] = "Fleca",
        ["night_club"] = "Local nocturn",
        ["spa"] = "Spa",
        ["gym"] = "Gimnàs",
        ["hair_care"] = "Perruqueria",
        ["beauty_salon"] = "Saló de bellesa"
    };

    internal static IReadOnlyCollection<string> ToFeatureChips(
        IEnumerable<string>? googleTypes,
        PlaceExternalDetailsDto details,
        string zuppetoTypeLabel)
    {
        var chips = new List<string>();
        if (details.AllowsDogs == true)
        {
            chips.Add("Gossos permesos");
        }
        else if (details.AllowsDogs == false)
        {
            chips.Add("No es permeten gossos");
        }

        if (details.OutdoorSeating == true)
        {
            chips.Add("Terrassa");
        }

        if (details.Reservable == true)
        {
            chips.Add("Reserva");
        }

        if (details.Takeout == true)
        {
            chips.Add("Per emportar");
        }

        if (details.Restroom == true)
        {
            chips.Add("Lavabo");
        }

        if (details.GoodForChildren == true)
        {
            chips.Add("Apta per nens");
        }

        foreach (var type in googleTypes ?? [])
        {
            if (SkippedTypes.Contains(type))
            {
                continue;
            }

            var label = TypeLabels.GetValueOrDefault(type);
            if (!string.IsNullOrWhiteSpace(label) && !chips.Contains(label, StringComparer.OrdinalIgnoreCase))
            {
                chips.Add(label);
            }
        }

        if (chips.Count == 0 && !string.IsNullOrWhiteSpace(zuppetoTypeLabel))
        {
            chips.Add(zuppetoTypeLabel.Trim());
        }

        return chips;
    }

    internal static IReadOnlyCollection<string> MergeConfirmedChips(
        IReadOnlyCollection<string> existing,
        IEnumerable<string>? confirmed)
    {
        var chips = existing.ToList();
        foreach (var chip in confirmed ?? [])
        {
            var label = chip.Trim();
            if (label.Length == 0 || chips.Contains(label, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            chips.Add(label);
        }

        return chips;
    }

    internal static IReadOnlyCollection<string> ToContextTags(string? neighborhood, string? city, string zuppetoTypeLabel)
    {
        var tags = new List<string>();
        if (!string.IsNullOrWhiteSpace(neighborhood))
        {
            tags.Add(neighborhood.Trim());
        }

        if (!string.IsNullOrWhiteSpace(city) && !tags.Contains(city.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var cityLabel = city.Trim();
            if (cityLabel.Contains(' '))
            {
                tags.Add(cityLabel);
            }
        }

        if (!string.IsNullOrWhiteSpace(zuppetoTypeLabel)
            && !tags.Contains(zuppetoTypeLabel.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            tags.Add(zuppetoTypeLabel.Trim());
        }

        return tags;
    }

    internal static string? NeighborhoodFromAddress(string? formattedAddress, string? currentNeighborhood)
    {
        if (!string.IsNullOrWhiteSpace(currentNeighborhood))
        {
            return currentNeighborhood.Trim();
        }

        var address = formattedAddress?.Trim() ?? string.Empty;
        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            return string.Empty;
        }

        // "street, Ciutat Vella, 08002 Barcelona, Spain" → district before postcode.
        for (var i = 1; i < parts.Length - 1; i++)
        {
            if (parts[i + 1].Length >= 5 && char.IsDigit(parts[i + 1][0]) && !char.IsDigit(parts[i][0]))
            {
                return parts[i];
            }
        }

        return string.Empty;
    }
}
