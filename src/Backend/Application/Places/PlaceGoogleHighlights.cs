namespace Zuppeto.Application.Places;

/// <summary>
/// Maps Google Place types and amenity flags to public chips. Open for new type rows; closed for ad-hoc ifs.
/// </summary>
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

    private static readonly GoogleTypeProfile[] TypeProfiles =
    [
        new("pet_store", "Botiga d'animals", ["Pinso", "Accessoris", "Productes per a mascotes"], IsGeneric: false),
        new("veterinary_care", "Veterinària", ["Atenció veterinària"], IsGeneric: false),
        new("pet_care", "Cura d'animals", ["Servei per a mascotes"], IsGeneric: false),
        new("dog_park", "Parc per a gossos", ["Espai per passejar"], IsGeneric: false),
        new("park", "Parc", [], IsGeneric: false),
        new("zoo", "Zoològic", [], IsGeneric: false),
        new("restaurant", "Restaurant", [], IsGeneric: false),
        new("cafe", "Cafeteria", [], IsGeneric: false),
        new("bar", "Bar", [], IsGeneric: false),
        new("meal_takeaway", "Per emportar", [], IsGeneric: false),
        new("meal_delivery", "Entrega a domicili", [], IsGeneric: false),
        new("lodging", "Allotjament", [], IsGeneric: false),
        new("hotel", "Hotel", [], IsGeneric: false),
        new("store", "Botiga", [], IsGeneric: true),
        new("clothing_store", "Botiga de roba", [], IsGeneric: false),
        new("grocery_or_supermarket", "Supermercat", [], IsGeneric: false),
        new("supermarket", "Supermercat", [], IsGeneric: false),
        new("bakery", "Fleca", [], IsGeneric: false),
        new("night_club", "Local nocturn", [], IsGeneric: false),
        new("spa", "Spa", [], IsGeneric: false),
        new("gym", "Gimnàs", [], IsGeneric: false),
        new("hair_care", "Perruqueria", [], IsGeneric: false),
        new("beauty_salon", "Saló de bellesa", [], IsGeneric: false)
    ];

    private static readonly HashSet<string> CategoryChips = new(
        TypeProfiles.Select(profile => profile.CategoryChip),
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> PetFamilyChips = new(
        TypeProfiles
            .Where(profile => profile.TypeKey is "pet_store" or "veterinary_care" or "pet_care" or "dog_park")
            .SelectMany(profile => profile.ExtraChips.Prepend(profile.CategoryChip)),
        StringComparer.OrdinalIgnoreCase);

    internal static IReadOnlyCollection<string> ToFeatureChips(
        PlaceExternalDetailsDto details,
        string? storedName = null,
        string? extraEvidence = null)
    {
        var chips = new List<string>();
        AddAmenityChips(chips, details);

        var profile = ResolveTypeProfile(details, storedName, extraEvidence);
        if (profile is not null)
        {
            AddChip(chips, profile.Value.CategoryChip);
            foreach (var extra in profile.Value.ExtraChips)
            {
                AddChip(chips, extra);
            }
        }
        else if (IsUsableDisplayName(details.PrimaryTypeDisplayName)
            && !LooksLikeFoodDisplayName(details.PrimaryTypeDisplayName))
        {
            AddChip(chips, details.PrimaryTypeDisplayName!);
        }

        return chips;
    }

    private static readonly HashSet<string> FoodTypeKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "bakery",
        "restaurant",
        "cafe",
        "bar",
        "meal_takeaway",
        "meal_delivery"
    };

    private static GoogleTypeProfile? ResolveTypeProfile(
        PlaceExternalDetailsDto details,
        string? storedName,
        string? extraEvidence)
    {
        var keys = DistinctTypeKeys(details.Types, details.PrimaryType);
        var evidence =
            $"{storedName} {details.Name} {details.PrimaryTypeDisplayName} {details.EditorialSummary} {extraEvidence}";

        if (LooksLikeVeterinary(evidence) || keys.Contains("veterinary_care"))
        {
            return ProfileByKey("veterinary_care");
        }

        if (LooksLikePetShop(evidence) || keys.Contains("pet_store") || keys.Contains("pet_care"))
        {
            return keys.Contains("pet_care") && !LooksLikePetShop(evidence) && !keys.Contains("pet_store")
                ? ProfileByKey("pet_care")
                : ProfileByKey("pet_store");
        }

        foreach (var profile in TypeProfiles)
        {
            if (profile.IsGeneric || FoodTypeKeys.Contains(profile.TypeKey) || !keys.Contains(profile.TypeKey))
            {
                continue;
            }

            return profile;
        }

        foreach (var profile in TypeProfiles)
        {
            if (!profile.IsGeneric && FoodTypeKeys.Contains(profile.TypeKey) && keys.Contains(profile.TypeKey))
            {
                return profile;
            }
        }

        var generic = TypeProfiles.FirstOrDefault(profile => profile.IsGeneric && keys.Contains(profile.TypeKey));
        return generic.TypeKey is null ? null : generic;
    }

    private static GoogleTypeProfile? ProfileByKey(string typeKey)
    {
        var profile = TypeProfiles.FirstOrDefault(item => item.TypeKey == typeKey);
        return profile.TypeKey is null ? null : profile;
    }

    internal static string CategoryLabel(IEnumerable<string> features, string zuppetoTypeLabel)
    {
        foreach (var feature in features)
        {
            var value = feature.Trim();
            if (value.Length > 0 && PetFamilyChips.Contains(value))
            {
                return value;
            }
        }

        foreach (var feature in features)
        {
            var value = feature.Trim();
            if (value.Length > 0 && CategoryChips.Contains(value))
            {
                return value;
            }
        }

        if (!string.IsNullOrWhiteSpace(zuppetoTypeLabel)
            && !zuppetoTypeLabel.Equals("Servei", StringComparison.OrdinalIgnoreCase))
        {
            return zuppetoTypeLabel.Trim();
        }

        return string.Empty;
    }

    internal static bool NeedsTypeInterpretation(IReadOnlyCollection<string> features)
    {
        if (features.Count == 0)
        {
            return true;
        }

        return features.All(feature => feature.Trim().Equals("Servei", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// True when chips must be rebuilt: empty/Servei, or a pet-shop name still tagged as bakery/etc.
    /// </summary>
    internal static bool NeedsChipRefresh(
        IReadOnlyCollection<string> features,
        string? placeName,
        string? description = null)
    {
        if (NeedsTypeInterpretation(features))
        {
            return true;
        }

        var evidence = $"{placeName} {description}";
        if (!LooksLikeVeterinary(evidence) && !LooksLikePetShop(evidence))
        {
            return false;
        }

        return !HasPetFamilyChip(features);
    }

    internal static IReadOnlyCollection<string> MergeConfirmedChips(
        IReadOnlyCollection<string> existing,
        IEnumerable<string>? confirmed)
    {
        var chips = existing.ToList();
        foreach (var chip in confirmed ?? [])
        {
            AddChip(chips, chip);
        }

        return chips;
    }

    internal static IReadOnlyCollection<string> ToContextTags(string? neighborhood, string zuppetoTypeLabel)
    {
        var tags = new List<string>();
        if (!string.IsNullOrWhiteSpace(neighborhood))
        {
            tags.Add(neighborhood.Trim());
        }

        if (!string.IsNullOrWhiteSpace(zuppetoTypeLabel)
            && !zuppetoTypeLabel.Equals("Servei", StringComparison.OrdinalIgnoreCase)
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

    private static void AddAmenityChips(List<string> chips, PlaceExternalDetailsDto details)
    {
        if (details.AllowsDogs == true)
        {
            AddChip(chips, "Gossos permesos");
        }
        else if (details.AllowsDogs == false)
        {
            AddChip(chips, "No es permeten gossos");
        }

        if (details.OutdoorSeating == true)
        {
            AddChip(chips, "Terrassa");
        }

        if (details.Reservable == true)
        {
            AddChip(chips, "Reserva");
        }

        if (details.Takeout == true)
        {
            AddChip(chips, "Per emportar");
        }

        if (details.Restroom == true)
        {
            AddChip(chips, "Lavabo");
        }

        if (details.GoodForChildren == true)
        {
            AddChip(chips, "Apta per nens");
        }
    }

    private static HashSet<string> DistinctTypeKeys(IEnumerable<string>? googleTypes, string? primaryType)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in (googleTypes ?? []).Append(primaryType ?? string.Empty))
        {
            var key = NormalizeTypeKey(raw);
            if (key.Length == 0 || SkippedTypes.Contains(key))
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

        var builder = new System.Text.StringBuilder(value.Length + 4);
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

    private static bool IsUsableDisplayName(string? name)
    {
        var value = name?.Trim() ?? string.Empty;
        if (value.Length < 3 || value.Contains('_', StringComparison.Ordinal))
        {
            return false;
        }

        return !SkippedTypes.Contains(NormalizeTypeKey(value));
    }

    private static bool HasPetFamilyChip(IEnumerable<string> features) =>
        features.Any(feature => PetFamilyChips.Contains(feature.Trim()));

    private static bool LooksLikeVeterinary(string? text) =>
        ContainsFolded(text, "veterinar");

    private static bool LooksLikePetShop(string? text) =>
        ContainsFolded(
            text,
            "botiga danimals",
            "tienda de animales",
            "pet store",
            "pet shop",
            "animaleria",
            "danimals",
            "de animales");

    private static bool LooksLikeFoodDisplayName(string? text) =>
        ContainsFolded(text, "fleca", "bakery", "forn", "panader");

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

    private static void AddChip(List<string> chips, string? label)
    {
        var value = label?.Trim() ?? string.Empty;
        if (value.Length == 0 || chips.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        chips.Add(value);
    }

    private readonly record struct GoogleTypeProfile(
        string TypeKey,
        string CategoryChip,
        string[] ExtraChips,
        bool IsGeneric);
}
