namespace Zuppeto.Application.Places;

/// <summary>
/// User-facing copy rules for catalog fields that may still hold internal Google placeholders.
/// </summary>
internal static class PlacePublicCopy
{
    internal const string UnspecifiedPetPolicyLabel = "Unspecified";

    internal static bool IsPublicPetPolicyLabel(string? label)
    {
        var value = label?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            return false;
        }

        if (value.Equals(UnspecifiedPetPolicyLabel, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (value.Contains("Google", StringComparison.OrdinalIgnoreCase)
            || value.Contains("cache", StringComparison.OrdinalIgnoreCase)
            || value.Contains("place_id", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    internal static string SanitizePetPolicyLabel(string? label)
    {
        return IsPublicPetPolicyLabel(label) ? label!.Trim() : string.Empty;
    }

    internal static string SanitizeDescription(string? description, string fallback)
    {
        var value = description?.Trim() ?? string.Empty;
        if (value.Length == 0 || IsInternalGoogleCopy(value))
        {
            return fallback.Trim();
        }

        return value;
    }

    internal static string PublicNarrative(string? description, string name, string? address, string? city = null)
    {
        var value = SanitizeDescription(description, string.Empty);
        if (value.Length == 0)
        {
            return string.Empty;
        }

        if (value.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var line = address?.Trim() ?? string.Empty;
        if (line.Length > 0 && value.Equals(line, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (IsLocationStub(value, city))
        {
            return string.Empty;
        }

        return value;
    }

    /// <summary>
    /// What the venue is (from Google types) plus editorial and official-website lead copy. Never city/address stubs.
    /// </summary>
    internal static string ComposeQuickContext(
        string? categoryLabel,
        string? editorial,
        string? websiteLead,
        string name,
        string? address,
        string? city)
    {
        var parts = new List<string>();
        var categorySentence = CategorySentence(categoryLabel);
        if (categorySentence.Length > 0)
        {
            parts.Add(categorySentence);
        }

        AddDistinctNarrative(parts, editorial, name, address, city);
        AddDistinctNarrative(parts, websiteLead, name, address, city);
        return string.Join(" ", parts);
    }

    /// <summary>
    /// True when copy only restates type + city (e.g. "Servei a 08002 Barcelona."), already on the address block.
    /// </summary>
    internal static bool IsLocationStub(string value, string? city)
    {
        var cityPart = city?.Trim() ?? string.Empty;
        if (cityPart.Length == 0)
        {
            return false;
        }

        var folded = value.Trim().TrimEnd('.').Trim();
        var suffix = " a " + cityPart;
        return folded.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            && folded.Length <= suffix.Length + 24;
    }

    internal static bool IsPlaceholderPricing(string? pricingLabel)
    {
        var value = pricingLabel?.Trim() ?? string.Empty;
        return value.Length == 0 || value == "—";
    }

    internal static string PublicPricingLabel(string? pricingLabel)
    {
        return IsPlaceholderPricing(pricingLabel) ? string.Empty : pricingLabel!.Trim();
    }

    private static void AddDistinctNarrative(
        List<string> parts,
        string? raw,
        string name,
        string? address,
        string? city)
    {
        var value = PublicNarrative(raw, name, address, city);
        if (value.Length == 0)
        {
            return;
        }

        foreach (var part in parts)
        {
            if (value.Contains(part.TrimEnd('.'), StringComparison.OrdinalIgnoreCase)
                || part.Contains(value.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        parts.Add(value);
    }

    private static string CategorySentence(string? categoryLabel)
    {
        var category = categoryLabel?.Trim() ?? string.Empty;
        if (category.Length == 0 || category.Equals("Servei", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var article = CatalanIndefiniteArticle(category);
        var noun = char.ToLowerInvariant(category[0]) + category[1..];
        return $"És {article} {noun}.";
    }

    private static string CatalanIndefiniteArticle(string nounPhrase)
    {
        var first = nounPhrase.Trim().Split(' ', 2)[0].ToLowerInvariant();
        return first switch
        {
            "botiga" or "veterinària" or "veterinaria" or "cafeteria" or "fleca"
                or "perruqueria" or "cura" => "una",
            _ => "un"
        };
    }

    private static bool IsInternalGoogleCopy(string value)
    {
        return value.Contains("Google Places", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Candidat extern", StringComparison.OrdinalIgnoreCase)
            || value.Contains("place_id", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Resultat Google", StringComparison.OrdinalIgnoreCase);
    }
}
