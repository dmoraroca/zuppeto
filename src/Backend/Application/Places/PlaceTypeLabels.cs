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
            PlaceType.FoodAndDrink => "Restauració",
            PlaceType.Accommodation => "Allotjament",
            PlaceType.Retail => "Comerç",
            PlaceType.Health => "Salut",
            PlaceType.Mobility => "Mobilitat",
            PlaceType.Outdoor => "Espai exterior",
            PlaceType.PetCare => "Cura d’animals",
            PlaceType.Entertainment => "Oci",
            _ => "Servei"
        };
    }
}
