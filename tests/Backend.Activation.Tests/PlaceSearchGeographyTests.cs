using Zuppeto.Application.Places;
using Xunit;

namespace Zuppeto.Backend.Activation.Tests;

public sealed class PlaceSearchGeographyTests
{
    [Fact]
    public void Normalize_PreservesValidViewportAndCenter()
    {
        var result = PlaceSearchGeographyPolicy.Normalize(Request(
            centerLatitude: 41.58m, centerLongitude: 2.55m,
            north: 41.61m, south: 41.55m, east: 2.60m, west: 2.50m));

        Assert.True(result.HasViewport);
        Assert.Equal(41.58m, result.CenterLatitude);
        Assert.Equal(2.55m, result.CenterLongitude);
    }

    [Fact]
    public void Normalize_DropsImpossibleOrInvertedCoordinates()
    {
        var result = PlaceSearchGeographyPolicy.Normalize(Request(
            centerLatitude: 95m, centerLongitude: -475.31m,
            north: 41m, south: 42m, east: -181m, west: 2m));

        Assert.False(result.HasViewport);
        Assert.Null(result.CenterLatitude);
        Assert.Null(result.CenterLongitude);
    }

    private static PlaceSearchRequest Request(
        decimal? centerLatitude, decimal? centerLongitude,
        decimal? north, decimal? south, decimal? east, decimal? west) =>
        new(null, null, null, null, "All", CenterLatitude: centerLatitude,
            CenterLongitude: centerLongitude, North: north, South: south, East: east, West: west);
}
