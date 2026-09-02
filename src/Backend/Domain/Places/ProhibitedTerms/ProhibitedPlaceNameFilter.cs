using System.Globalization;
using System.Text;

namespace Zuppeto.Domain.Places.ProhibitedTerms;

/// <summary>
/// Internal listing gate: a place name that hits any catalog term is not shown.
/// </summary>
public sealed class ProhibitedPlaceNameFilter(IProhibitedPlaceTermsCatalogFactory catalogFactory)
{
    public bool IsProhibited(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var folded = Fold(name);
        var compact = Compact(folded);

        foreach (var catalog in catalogFactory.CreateAll())
        {
            foreach (var term in catalog.Terms)
            {
                var foldedTerm = Fold(term);
                if (foldedTerm.Length == 0)
                {
                    continue;
                }

                if (folded.Contains(foldedTerm, StringComparison.Ordinal) ||
                    compact.Contains(Compact(foldedTerm), StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string Fold(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string Compact(string value) =>
        string.Concat(value.Where(character => !char.IsWhiteSpace(character)));
}
