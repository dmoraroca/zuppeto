using System.Text.Json;

namespace Zuppeto.Infrastructure.Places.OvertureMaps;

internal sealed record OvertureCategoryMapping(
    string? PetiLocCategory,
    string OriginalCategory,
    string? OriginalSubcategory,
    IReadOnlyList<string> OriginalHierarchy,
    string AppliedRule,
    bool RequiresClassificationReview);

internal static class OverturePlaceCategoryMapper
{
    internal static readonly string[] PetiLocCategories =
    [
        "restaurant", "cafe", "bar", "pub", "fast_food", "bakery", "ice_cream", "hotel", "hostel",
        "apartment", "camping", "park", "shop", "veterinary", "pharmacy", "fuel", "pet_grooming",
        "pet_service", "service"
    ];

    internal static OvertureCategoryMapping Map(JsonElement properties)
    {
        var basic = Text(properties, "basic_category") ?? string.Empty;
        var primary = properties.TryGetProperty("taxonomy", out var taxonomy)
            ? Text(taxonomy, "primary") ?? string.Empty : string.Empty;
        var hierarchy = properties.TryGetProperty("taxonomy", out taxonomy)
            && taxonomy.TryGetProperty("hierarchy", out var values)
            && values.ValueKind == JsonValueKind.Array
                ? values.EnumerateArray().Select(value => value.GetString() ?? string.Empty)
                : [];
        var categories = hierarchy.Prepend(primary).Prepend(basic)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(Normalize).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var mapped = ExplicitMappings.FirstOrDefault(mapping =>
            categories.Any(mapping.SourceCategories.Contains));

        var original = !string.IsNullOrWhiteSpace(basic) ? basic
            : !string.IsNullOrWhiteSpace(primary) ? primary : "unknown";
        return new(mapped?.PetiLocCategory, original,
            string.IsNullOrWhiteSpace(primary) ? null : primary,
            categories, mapped?.Rule ?? "unmapped.pending_review", mapped is null);
    }

    private sealed record ExplicitCategoryMapping(
        string PetiLocCategory, HashSet<string> SourceCategories, string Rule);

    private static readonly ExplicitCategoryMapping[] ExplicitMappings =
    [
        Mapping("pet_grooming", "animal.pet_grooming", "pet_grooming", "dog_grooming", "pet_groomer"),
        Mapping("pet_service", "animal.pet_service", "pet_service", "pet_services", "animal_or_pet_service",
            "animal_boarding", "pet_boarding", "kennel", "dog_training", "pet_training", "dog_trainer",
            "dog_walking", "dog_walker", "animal_shelter", "animal_rescue", "pet_adoption"),
        Mapping("veterinary", "animal.veterinary", "veterinary", "veterinarian", "animal_hospital"),
        Mapping("pharmacy", "health.pharmacy", "pharmacy", "pharmacy_and_drug_store"),
        Mapping("fuel", "mobility.fuel", "fuel", "gas_station", "petrol_station", "truck_stop",
            "electric_vehicle_charging_station"),
        Mapping("bakery", "food.bakery", "bakery"),
        Mapping("ice_cream", "food.ice_cream", "ice_cream", "ice_cream_shop"),
        Mapping("fast_food", "food.fast_food", "fast_food", "fast_food_restaurant"),
        Mapping("cafe", "food.cafe", "cafe", "coffee_shop"),
        Mapping("pub", "food.pub", "pub", "gastropub", "nightclub"),
        Mapping("bar", "food.bar", "bar", "biergarten", "beer_garden"),
        Mapping("restaurant", "food.restaurant", "restaurant", "food_court"),
        Mapping("hostel", "lodging.hostel", "hostel", "guest_house", "pension", "bed_and_breakfast"),
        Mapping("apartment", "lodging.apartment", "apartment", "apartment_hotel", "vacation_rental",
            "private_lodging", "holiday_rental_home"),
        Mapping("camping", "lodging.camping", "camping", "campground", "rv_park"),
        Mapping("hotel", "lodging.hotel", "hotel", "motel", "resort"),
        Mapping("park", "outdoor.park", "park", "urban_park", "natural_park", "national_park",
            "nature_reserve", "dog_park", "recreation_ground", "playground", "public_garden"),
        Mapping("shop", "retail.shop", "shop", "store", "shopping", "supermarket", "grocery_store",
            "convenience_store", "shopping_center", "department_store"),
    ];

    private static ExplicitCategoryMapping Mapping(string category, string rule, params string[] sourceCategories) =>
        new(category, sourceCategories.ToHashSet(StringComparer.OrdinalIgnoreCase), rule);

    private static string Normalize(string value) => value.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
    private static string? Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() : null;
}
