using System.Text.Json;

namespace Zuppeto.Infrastructure.Places.OvertureMaps;

internal sealed record OvertureCategoryMapping(
    string PetiLocCategory,
    string OriginalCategory,
    string? OriginalSubcategory,
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

        string? Match(params string[] needles) => categories.FirstOrDefault(category =>
            needles.Any(needle => category.Equals(needle, StringComparison.OrdinalIgnoreCase)
                || category.EndsWith($"_{needle}", StringComparison.OrdinalIgnoreCase)));

        var mapped = Match("pet_grooming", "dog_grooming") is not null ? "pet_grooming"
            : Match("veterinary", "veterinarian", "animal_hospital") is not null ? "veterinary"
            : Match("pet_service", "pet_services") is not null ? "pet_service"
            : Match("pharmacy", "pharmacy_and_drug_store") is not null ? "pharmacy"
            : Match("fuel", "gas_station", "petrol_station") is not null ? "fuel"
            : Match("bakery") is not null ? "bakery"
            : Match("ice_cream", "ice_cream_shop") is not null ? "ice_cream"
            : Match("fast_food", "fast_food_restaurant") is not null ? "fast_food"
            : Match("cafe", "coffee_shop") is not null ? "cafe"
            : Match("pub", "gastropub", "nightclub") is not null ? "pub"
            : Match("bar", "biergarten", "beer_garden") is not null ? "bar"
            : Match("restaurant", "food_court") is not null ? "restaurant"
            : Match("hostel", "guest_house", "pension", "bed_and_breakfast") is not null ? "hostel"
            : Match("apartment", "apartment_hotel", "vacation_rental", "private_lodging") is not null ? "apartment"
            : Match("camping", "campground", "rv_park") is not null ? "camping"
            : Match("hotel", "motel", "resort") is not null ? "hotel"
            : Match("park", "dog_park") is not null ? "park"
            : Match("shop", "store", "shopping", "supermarket", "grocery_store", "convenience_store") is not null ? "shop"
            : "service";

        var original = !string.IsNullOrWhiteSpace(basic) ? basic
            : !string.IsNullOrWhiteSpace(primary) ? primary : "unknown";
        return new(mapped, original, string.IsNullOrWhiteSpace(primary) ? null : primary, mapped == "service");
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
    private static string? Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() : null;
}
