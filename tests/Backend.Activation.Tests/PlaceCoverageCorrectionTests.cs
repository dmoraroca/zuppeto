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

        Assert.Equal(PlaceType.FoodAndDrink, place.Type);
        Assert.Equal("bakery", record.CommercialCategory);
        Assert.Equal("Fleca", CommercialCategoryLabels.From(place.CommercialCategory, place.Type));
    }

    [Theory]
    [InlineData("bakery", PlaceType.FoodAndDrink)]
    [InlineData("cafe", PlaceType.FoodAndDrink)]
    [InlineData("fast_food", PlaceType.FoodAndDrink)]
    [InlineData("camping", PlaceType.Accommodation)]
    [InlineData("pharmacy", PlaceType.Health)]
    [InlineData("fuel", PlaceType.Mobility)]
    [InlineData("shop", PlaceType.Retail)]
    [InlineData("veterinary", PlaceType.PetCare)]
    public void Commercial_categories_map_to_semantic_general_types(string category, PlaceType expected)
    {
        Assert.Equal(expected, PlaceCommercialClassification.GeneralType(category));
    }

    [Fact]
    public void Every_import_category_has_an_explicit_semantic_general_type()
    {
        var expected = new Dictionary<PlaceType, string[]>
        {
            [PlaceType.FoodAndDrink] = ["restaurant", "cafe", "bar", "pub", "fast_food", "ice_cream", "bakery", "biergarten", "food_court"],
            [PlaceType.Entertainment] = ["nightclub"],
            [PlaceType.Health] = ["pharmacy"],
            [PlaceType.Mobility] = ["fuel"],
            [PlaceType.Accommodation] = ["hotel", "hostel", "motel", "apartment", "camping"],
            [PlaceType.Outdoor] = ["park", "dog_park"],
            [PlaceType.Retail] = ["shop", "pet_shop"],
            [PlaceType.PetCare] = ["veterinary", "pet_service", "pet_grooming"],
            [PlaceType.Service] = ["service"]
        };
        var classified = expected.SelectMany(group => group.Value.Select(category => (category, group.Key))).ToArray();

        Assert.Equal(PlaceCatalogCategories.All.Order(), classified.Select(item => item.category).Order());
        Assert.All(classified, item => Assert.Equal(item.Key, PlaceCommercialClassification.GeneralType(item.category)));
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
        Guid.NewGuid(), "Local", PlaceCommercialClassification.GeneralType(category), "", "", "",
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
