using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

public static class PlaceCommercialClassification
{
    public static PlaceType GeneralType(string category) => Normalize(category) switch
    {
        "restaurant" or "cafe" or "bar" or "pub" or "fast_food" or "ice_cream" or "bakery"
            or "biergarten" or "food_court" => PlaceType.FoodAndDrink,
        "hotel" or "hostel" or "motel" or "apartment" or "camping" => PlaceType.Accommodation,
        "shop" or "pet_shop" => PlaceType.Retail,
        "pharmacy" => PlaceType.Health,
        "fuel" => PlaceType.Mobility,
        "park" or "dog_park" => PlaceType.Outdoor,
        "veterinary" or "pet_service" or "pet_grooming" => PlaceType.PetCare,
        "nightclub" => PlaceType.Entertainment,
        _ => PlaceType.Service
    };

    public static bool CategoriesAreCompatible(string candidate, string? existing) =>
        !string.IsNullOrWhiteSpace(existing)
        && string.Equals(Normalize(candidate), Normalize(existing), StringComparison.Ordinal);

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
