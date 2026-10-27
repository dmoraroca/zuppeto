using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

internal static class PlaceCatalogEnums
{
    internal static PlaceType? ParsePlaceType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return null;
        }

        return Enum.TryParse<PlaceType>(type, ignoreCase: true, out var parsed) ? parsed : null;
    }

    internal static PlaceType ParseRequiredPlaceType(string type)
    {
        return Enum.Parse<PlaceType>(type, ignoreCase: true);
    }

    internal static PetCategory ParsePetCategory(string petCategory)
    {
        if (string.IsNullOrWhiteSpace(petCategory))
        {
            return PetCategory.All;
        }

        return Enum.TryParse<PetCategory>(petCategory, ignoreCase: true, out var parsed)
            ? parsed
            : PetCategory.All;
    }

}
