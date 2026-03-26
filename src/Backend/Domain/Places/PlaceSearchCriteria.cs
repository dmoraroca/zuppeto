using YepPet.Domain.Common;

namespace YepPet.Domain.Places;

public sealed class PlaceSearchCriteria : ValueObject
{
    public PlaceSearchCriteria(
        string? searchText,
        string? city,
        PlaceType? type,
        PetCategory petCategory)
    {
        SearchText = Normalize(searchText);
        City = Normalize(city);
        Type = type;
        PetCategory = petCategory;
    }

    public string? SearchText { get; }

    public string? City { get; }

    public PlaceType? Type { get; }

    public PetCategory PetCategory { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SearchText;
        yield return City;
        yield return Type;
        yield return PetCategory;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
