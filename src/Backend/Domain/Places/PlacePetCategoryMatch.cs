using System.Text.RegularExpressions;

namespace Zuppeto.Domain.Places;

/// <summary>
/// Pet list filter. Many catalog rows have both flags true; a dog-only name
/// (e.g. Dog Care) must not appear under cats, and the reverse.
/// Mixed names (Gos i Gat) stay in both filters.
/// </summary>
public static class PlacePetCategoryMatch
{
    private static readonly Regex DogHint = new(
        @"\b(dogs?|dogg(?:y|ie)s?|pupp(?:y|ies)|perros?|gossos|gos|canina|canino|canins?|caní|kennels?)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex CatHint = new(
        @"\b(cats?|kitt(?:y|ies)|felines?|felina|felino|gats?|gatets?|gata)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool Fits(string name, bool acceptsDogs, bool acceptsCats, PetCategory category)
    {
        if (category == PetCategory.All)
        {
            return true;
        }

        if (category == PetCategory.Dogs)
        {
            return acceptsDogs && !IsCatExclusive(name);
        }

        if (category == PetCategory.Cats)
        {
            return acceptsCats && !IsDogExclusive(name);
        }

        return true;
    }

    public static bool IsDogExclusive(string name)
    {
        return HasDogHint(name) && !HasCatHint(name);
    }

    public static bool IsCatExclusive(string name)
    {
        return HasCatHint(name) && !HasDogHint(name);
    }

    private static bool HasDogHint(string name) => DogHint.IsMatch(name ?? string.Empty);

    private static bool HasCatHint(string name) => CatHint.IsMatch(name ?? string.Empty);
}
