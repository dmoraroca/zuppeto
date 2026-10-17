namespace Zuppeto.Application.Places;

internal static class CommercialCategoryLabels
{
    internal static string From(string? category, Domain.Places.PlaceType fallback) =>
        category?.Trim().ToLowerInvariant() switch
        {
            "restaurant" => "Restaurant",
            "cafe" => "Cafeteria",
            "bar" => "Bar",
            "pub" => "Pub",
            "fast_food" => "Menjar ràpid",
            "ice_cream" => "Gelateria",
            "bakery" => "Fleca",
            "hotel" => "Hotel",
            "hostel" => "Hostal o pensió",
            "motel" => "Motel",
            "apartment" => "Apartament turístic",
            "camping" => "Càmping",
            "park" => "Parc",
            "dog_park" => "Parc caní",
            "shop" => "Comerç",
            "pet_shop" => "Botiga per a mascotes",
            "veterinary" => "Veterinari",
            "pharmacy" => "Farmàcia",
            "fuel" => "Benzinera",
            "pet_service" => "Servei per a mascotes",
            "pet_grooming" or "dog_grooming" => "Perruqueria canina",
            "service" => "Altres serveis",
            _ => PlaceTypeLabels.From(fallback)
        };
}
