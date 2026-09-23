using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Places;

public sealed class PlaceSearchCriteria : ValueObject
{
    public PlaceSearchCriteria(
        string? searchText,
        string? country,
        string? city,
        PlaceType? type,
        PetCategory petCategory,
        Guid? countryId = null,
        Guid? territorialUnitId = null)
    {
        SearchText = Normalize(searchText);
        Country = Normalize(country);
        City = Normalize(city);
        Type = type;
        PetCategory = petCategory;
        CountryId = countryId;
        TerritorialUnitId = territorialUnitId;
    }

    public string? SearchText { get; }

    public string? Country { get; }

    public string? City { get; }

    public PlaceType? Type { get; }

    public PetCategory PetCategory { get; }
    public Guid? CountryId { get; }
    public Guid? TerritorialUnitId { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SearchText;
        yield return Country;
        yield return City;
        yield return Type;
        yield return PetCategory;
        yield return CountryId;
        yield return TerritorialUnitId;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
