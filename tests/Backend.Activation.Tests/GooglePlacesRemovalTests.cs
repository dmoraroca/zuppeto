using Zuppeto.Application.Places;
using Zuppeto.Application.Places.Validators;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ValueObjects;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class GooglePlacesRemovalTests
{
    [Fact]
    public void Active_google_places_infrastructure_is_absent()
    {
        var activeTypes = typeof(Zuppeto.Infrastructure.DependencyInjection).Assembly
            .GetTypes()
            .Where(type => string.Equals(
                type.Namespace,
                "Zuppeto.Infrastructure.GooglePlaces",
                StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(activeTypes);
    }

    [Fact]
    public void Historical_google_metadata_can_still_be_materialized()
    {
        var cachedUntil = DateTimeOffset.UtcNow.AddDays(-1);
        var lastSync = DateTimeOffset.UtcNow.AddDays(-31);
        var place = HistoricalPlace(cachedUntil, lastSync);

        Assert.Equal(PlaceDataProvenance.GooglePlaces, place.DataProvenance);
        Assert.Equal("historical-place-id", place.GooglePlaceId);
        Assert.Equal(cachedUntil, place.GoogleCoordinatesCachedUntil);
        Assert.Equal(lastSync, place.LastGoogleSyncAt);
        Assert.Null(typeof(Place).GetMethod("SetDataProvenance"));
    }

    [Fact]
    public void Admin_contract_rejects_new_google_metadata()
    {
        var request = new PlaceUpsertRequest(
            null, "Lloc", "Restaurant", "", "", "", "Carrer 1", "Barcelona", "Espanya", "",
            41m, 2m, false, false, "", "", "", 0m, 0, [], [],
            DataProvenance: nameof(PlaceDataProvenance.GooglePlaces),
            GooglePlaceId: "new-google-place-id");

        var result = new PlaceUpsertRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.Field == nameof(PlaceUpsertRequest.GooglePlaceId)
            && error.Message.Contains("només de lectura", StringComparison.OrdinalIgnoreCase));
    }

    private static Place HistoricalPlace(DateTimeOffset cachedUntil, DateTimeOffset lastSync) => new(
        Guid.NewGuid(),
        "Lloc històric",
        PlaceType.Restaurant,
        "Descripció",
        "Descripció històrica",
        "",
        new PostalAddress("Carrer 1", "Barcelona", "Espanya", ""),
        new GeoLocation(41m, 2m),
        new PetPolicy(false, false, "", ""),
        new Pricing(""),
        new RatingSnapshot(0m, 0),
        PlaceDataProvenance.GooglePlaces,
        "historical-place-id",
        cachedUntil,
        lastSync,
        excludeFromOsmMap: true);
}
