using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.Places.OpenStreetMap;

internal static class OsmPlaceCategoryMapper
{
    internal static readonly string[] SupportedCategories = PlaceCatalogCategories.All
        .Where(category => category is not ("fuel" or "service"))
        .ToArray();

    internal static bool TryMap(IReadOnlyDictionary<string, string> tags,
        IReadOnlySet<string> requestedCategories, out string category)
    {
        category = Candidates(tags).FirstOrDefault(requestedCategories.Contains) ?? string.Empty;
        return category.Length > 0;
    }

    internal static IReadOnlyCollection<(string Key, string Value)> Selectors(string category) => category switch
    {
        "restaurant" => [("amenity", "restaurant")],
        "cafe" => [("amenity", "cafe")],
        "bar" => [("amenity", "bar")],
        "pub" => [("amenity", "pub")],
        "fast_food" => [("amenity", "fast_food")],
        "biergarten" => [("amenity", "biergarten")],
        "food_court" => [("amenity", "food_court")],
        "nightclub" => [("amenity", "nightclub")],
        "pharmacy" => [("amenity", "pharmacy")],
        "ice_cream" => [("amenity", "ice_cream"), ("shop", "ice_cream")],
        "bakery" => [("shop", "bakery")],
        "hotel" => [("tourism", "hotel")],
        "hostel" => [("tourism", "hostel"), ("tourism", "guest_house")],
        "motel" => [("tourism", "motel")],
        "apartment" => [("tourism", "apartment")],
        "camping" => [("tourism", "camp_site"), ("tourism", "caravan_site")],
        "park" => [("leisure", "park")],
        "dog_park" => [("leisure", "dog_park")],
        "shop" => [("shop", ".+")],
        "pet_shop" => [("shop", "pet")],
        "veterinary" => [("amenity", "veterinary")],
        "pet_service" => [("amenity", "animal_shelter"), ("amenity", "animal_boarding")],
        "pet_grooming" => [("shop", "pet_grooming")],
        _ => []
    };

    private static IEnumerable<string> Candidates(IReadOnlyDictionary<string, string> tags)
    {
        var amenity = tags.GetValueOrDefault("amenity");
        if (amenity is "restaurant" or "cafe" or "bar" or "pub" or "fast_food" or "ice_cream" or "biergarten"
            or "food_court" or "nightclub" or "veterinary" or "pharmacy") yield return amenity;
        if (amenity is "animal_shelter" or "animal_boarding") yield return "pet_service";

        var tourism = tags.GetValueOrDefault("tourism");
        if (tourism == "hotel") yield return "hotel";
        if (tourism is "hostel" or "guest_house") yield return "hostel";
        if (tourism == "motel") yield return "motel";
        if (tourism == "apartment") yield return "apartment";
        if (tourism is "camp_site" or "caravan_site") yield return "camping";

        var leisure = tags.GetValueOrDefault("leisure");
        if (leisure == "dog_park") yield return "dog_park";
        if (leisure == "park") yield return "park";

        var shop = tags.GetValueOrDefault("shop");
        if (shop == "pet_grooming") yield return "pet_grooming";
        if (shop == "pet") yield return "pet_shop";
        if (shop == "bakery") yield return "bakery";
        if (shop == "ice_cream") yield return "ice_cream";
        if (!string.IsNullOrWhiteSpace(shop) && shop is not ("pet" or "pet_grooming" or "bakery" or "ice_cream")) yield return "shop";
    }
}
