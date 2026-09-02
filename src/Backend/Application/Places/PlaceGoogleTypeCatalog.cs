namespace Zuppeto.Application.Places;

/// <summary>
/// Google type → public chips. Open for new rows; closed for ad-hoc ifs.
/// </summary>
internal static class PlaceGoogleTypeCatalog
{
    internal static readonly PlaceGoogleTypeProfile[] Profiles =
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

    internal static readonly HashSet<string> SkippedTypes = new(StringComparer.OrdinalIgnoreCase)
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

    internal static readonly HashSet<string> FoodTypeKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "bakery",
        "restaurant",
        "cafe",
        "bar",
        "meal_takeaway",
        "meal_delivery"
    };

    internal static readonly HashSet<string> CategoryChips = new(
        Profiles.Select(profile => profile.CategoryChip),
        StringComparer.OrdinalIgnoreCase);

    internal static readonly HashSet<string> PetFamilyChips = new(
        Profiles
            .Where(profile => profile.TypeKey is "pet_store" or "veterinary_care" or "pet_care" or "dog_park")
            .SelectMany(profile => profile.ExtraChips.Prepend(profile.CategoryChip)),
        StringComparer.OrdinalIgnoreCase);

    internal static PlaceGoogleTypeProfile? ByKey(string typeKey)
    {
        foreach (var profile in Profiles)
        {
            if (profile.TypeKey == typeKey)
            {
                return profile;
            }
        }

        return null;
    }
}

internal readonly record struct PlaceGoogleTypeProfile(
    string TypeKey,
    string CategoryChip,
    string[] ExtraChips,
    bool IsGeneric);
