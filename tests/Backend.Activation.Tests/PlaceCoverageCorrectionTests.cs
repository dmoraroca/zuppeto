using Zuppeto.Application.Places;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ValueObjects;
using Zuppeto.Infrastructure.Persistence.Entities;
using Zuppeto.Infrastructure.Persistence.Mappings;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class PlaceCoverageCorrectionTests
{
    [Fact]
    public void Persistence_preserves_explicit_map_exclusion_even_with_coordinates()
    {
        var record = Record();
        record.ExcludeFromOsmMap = true;

        Assert.True(PlacePersistenceMapper.ToDomain(record).ExcludeFromOsmMap);
    }

    [Fact]
    public void Commercial_category_is_independent_from_general_place_type()
    {
        var place = Place("bakery");
        var record = PlacePersistenceMapper.ToRecord(place);

        Assert.Equal(PlaceType.Bar, place.Type);
        Assert.Equal("bakery", record.CommercialCategory);
        Assert.Equal("Fleca", CommercialCategoryLabels.From(place.CommercialCategory, place.Type));
    }

    [Fact]
    public void Publishing_syncs_terrace_feature_without_changing_unknown_pet_policy()
    {
        var place = Place("restaurant");
        place.ReplaceFeatures(["Terrassa"]);
        var record = PlacePersistenceMapper.ToRecord(place);

        PlacePersistenceMapper.SyncCollections(place, record);

        Assert.Contains(record.PlaceFeatures, link => link.Feature.Code == "Terrassa");
        Assert.Equal(PetAccessStatus.Unknown, place.PetPolicy.Status);
        Assert.False(place.PetPolicy.AcceptsDogs);
    }

    private static Place Place(string category) => new(
        Guid.NewGuid(), "Local", PlaceType.Bar, "", "", "",
        new PostalAddress("Carrer 1", "Arenys de Mar", "Espanya", ""),
        new GeoLocation(41.58m, 2.55m),
        new PetPolicy(PetAccessStatus.Unknown, [], [], "Desconeguda", ""),
        new Pricing(""), new RatingSnapshot(0, 0), commercialCategory: category);

    private static PlaceRecord Record() => new()
    {
        Id = Guid.NewGuid(), Name = "Local", Type = "Restaurant", CommercialCategory = "restaurant",
        ShortDescription = "", Description = "", CoverImageUrl = "", AddressLine1 = "Carrer 1",
        City = "Arenys de Mar", Country = "Espanya", Latitude = 41.58m, Longitude = 2.55m,
        PetPolicyStatus = "Unknown", PetSpeciesJson = "[]", PetConditionsJson = "[]",
        PetPolicyLabel = "", PricingLabel = "", DataProvenance = "OpenStreetMap"
    };
}
