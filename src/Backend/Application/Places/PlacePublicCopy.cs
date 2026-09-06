using System.Text.RegularExpressions;

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
        var value = StripInventedCategoryLead(description?.Trim() ?? string.Empty);
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
    /// Editorial and official-website lead copy only. Never invents a category sentence.
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
        var value = PublicNarrative(StripInventedCategoryLead(raw), name, address, city);
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

    /// <summary>
    /// Drops the old generated lead ("És una veterinària.") so stored copy is only real text.
    /// </summary>
    internal static string StripInventedCategoryLead(string? value)
    {
        var text = value?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            return string.Empty;
        }

        return InventedCategoryLead.Replace(text, string.Empty).Trim();
    }

    private static readonly Regex InventedCategoryLead = new(
        @"^És (una|un) [^\.]{1,60}\.\s*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static bool IsInternalGoogleCopy(string value)
    {
        return value.Contains("Google Places", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Candidat extern", StringComparison.OrdinalIgnoreCase)
            || value.Contains("place_id", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Resultat Google", StringComparison.OrdinalIgnoreCase);
    }
}
