using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

internal static class PlaceTypeLabels
{
    internal static string From(PlaceType type)
    {
        return type switch
        {
            PlaceType.Bar => "Bar",
            PlaceType.Restaurant => "Restaurant",
            PlaceType.Hotel => "Hotel",
            PlaceType.Apartment => "Apartament",
            PlaceType.Park => "Parc",
            _ => "Servei"
        };
    }
}
