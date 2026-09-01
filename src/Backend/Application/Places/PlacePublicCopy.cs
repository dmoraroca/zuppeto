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

    internal static string PublicNarrative(string? description, string name, string? address)
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

        return value;
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

    private static bool IsInternalGoogleCopy(string value)
    {
        return value.Contains("Google Places", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Candidat extern", StringComparison.OrdinalIgnoreCase)
            || value.Contains("place_id", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Resultat Google", StringComparison.OrdinalIgnoreCase);
    }
}
