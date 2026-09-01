using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Zuppeto.Application.Places;

/// <summary>
/// Maps confirmed website wording to public chips. Open for new amenity rows; closed for ad-hoc ifs.
/// </summary>
public static class PlaceWebsiteAmenityCatalog
{
    private static readonly (string Pattern, string Chip)[] Amenities =
    [
        ("terrass|terraza|terrace", "Terrassa"),
        (@"\bjardi\b|jardin\b", "Jardí"),
        ("coctel|cocktail|cocteleria", "Cocteleria")
    ];

    public static IReadOnlyCollection<string> ConfirmedChips(string plainText)
    {
        var haystack = Normalize(plainText);
        if (haystack.Length < 12)
        {
            return [];
        }

        var chips = new List<string>();
        foreach (var (pattern, chip) in Amenities)
        {
            if (Regex.IsMatch(haystack, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                && !chips.Contains(chip, StringComparer.OrdinalIgnoreCase))
            {
                chips.Add(chip);
            }
        }

        return chips;
    }

    public static string? Summary(string plainText, int maxLength = 320)
    {
        var collapsed = Regex.Replace(plainText, @"\s+", " ").Trim();
        if (collapsed.Length < 40)
        {
            return null;
        }

        var limit = Math.Min(maxLength, collapsed.Length);
        var slice = collapsed[..limit];
        var lastStop = slice.LastIndexOfAny(['.', '!', '?']);
        if (lastStop >= 80)
        {
            slice = slice[..(lastStop + 1)];
        }

        return slice.Trim();
    }

    public static bool MentionsPlace(string plainText, string placeName)
    {
        var haystack = Normalize(plainText);
        foreach (var token in DistinctNameTokens(placeName))
        {
            if (haystack.Contains(token, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyCollection<string> DistinctNameTokens(string placeName)
    {
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "restaurant", "restaurante", "bar", "cafe", "hotel", "the", "el", "la", "de", "del", "d"
        };

        return Normalize(placeName)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 4 && !skip.Contains(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string Normalize(string value)
    {
        var form = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(form.Length);
        foreach (var ch in form)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
