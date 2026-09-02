using System.Text;

namespace Zuppeto.Application.Places;

/// <summary>
/// Strategy: one Google type profile from keys + name/editorial/website evidence.
/// Pet venues win over food types (bakery on a pet shop).
/// </summary>
internal static class PlaceGoogleTypeInterpreter
{
    internal static PlaceGoogleTypeProfile? Resolve(
        PlaceExternalDetailsDto details,
        string? storedName,
        string? extraEvidence)
    {
        var keys = DistinctTypeKeys(details.Types, details.PrimaryType);
        var evidence =
            $"{storedName} {details.Name} {details.PrimaryTypeDisplayName} {details.EditorialSummary} {extraEvidence}";

        if (LooksLikeVeterinary(evidence) || keys.Contains("veterinary_care"))
        {
            return PlaceGoogleTypeCatalog.ByKey("veterinary_care");
        }

        if (LooksLikePetShop(evidence) || keys.Contains("pet_store") || keys.Contains("pet_care"))
        {
            return keys.Contains("pet_care") && !LooksLikePetShop(evidence) && !keys.Contains("pet_store")
                ? PlaceGoogleTypeCatalog.ByKey("pet_care")
                : PlaceGoogleTypeCatalog.ByKey("pet_store");
        }

        foreach (var profile in PlaceGoogleTypeCatalog.Profiles)
        {
            if (profile.IsGeneric || PlaceGoogleTypeCatalog.FoodTypeKeys.Contains(profile.TypeKey)
                || !keys.Contains(profile.TypeKey))
            {
                continue;
            }

            return profile;
        }

        foreach (var profile in PlaceGoogleTypeCatalog.Profiles)
        {
            if (!profile.IsGeneric
                && PlaceGoogleTypeCatalog.FoodTypeKeys.Contains(profile.TypeKey)
                && keys.Contains(profile.TypeKey))
            {
                return profile;
            }
        }

        foreach (var profile in PlaceGoogleTypeCatalog.Profiles)
        {
            if (profile.IsGeneric && keys.Contains(profile.TypeKey))
            {
                return profile;
            }
        }

        return null;
    }

    internal static bool LooksLikeVeterinary(string? text) =>
        ContainsFolded(text, "veterinar");

    internal static bool LooksLikePetShop(string? text) =>
        ContainsFolded(
            text,
            "botiga danimals",
            "tienda de animales",
            "pet store",
            "pet shop",
            "animaleria",
            "danimals",
            "de animales");

    internal static bool LooksLikeFoodDisplayName(string? text) =>
        ContainsFolded(text, "fleca", "bakery", "forn", "panader");

    internal static bool IsUsableDisplayName(string? name)
    {
        var value = name?.Trim() ?? string.Empty;
        if (value.Length < 3 || value.Contains('_', StringComparison.Ordinal))
        {
            return false;
        }

        return !PlaceGoogleTypeCatalog.SkippedTypes.Contains(NormalizeTypeKey(value));
    }

    private static HashSet<string> DistinctTypeKeys(IEnumerable<string>? googleTypes, string? primaryType)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in (googleTypes ?? []).Append(primaryType ?? string.Empty))
        {
            var key = NormalizeTypeKey(raw);
            if (key.Length == 0 || PlaceGoogleTypeCatalog.SkippedTypes.Contains(key))
            {
                continue;
            }

            keys.Add(key);
        }

        return keys;
    }

    private static string NormalizeTypeKey(string? raw)
    {
        var value = raw?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            return string.Empty;
        }

        var slash = value.LastIndexOf('/');
        if (slash >= 0 && slash < value.Length - 1)
        {
            value = value[(slash + 1)..];
        }

        var dot = value.LastIndexOf('.');
        if (dot >= 0 && dot < value.Length - 1)
        {
            value = value[(dot + 1)..];
        }

        var builder = new StringBuilder(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (i > 0 && char.IsUpper(ch) && (char.IsLower(value[i - 1]) || char.IsDigit(value[i - 1])))
            {
                builder.Append('_');
            }

            builder.Append(ch);
        }

        return builder.ToString().Trim().ToLowerInvariant().Replace('-', '_');
    }

    private static bool ContainsFolded(string? haystack, params string[] needles)
    {
        var fold = (haystack ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace('’', '\'')
            .Replace('´', '\'')
            .Replace("'", string.Empty);
        if (fold.Length == 0)
        {
            return false;
        }

        return needles.Any(needle => fold.Contains(needle, StringComparison.Ordinal));
    }
}
