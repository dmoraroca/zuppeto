namespace Zuppeto.Application.Places;

public sealed record PlaceSearchGeography(
    decimal? CenterLatitude,
    decimal? CenterLongitude,
    decimal? North,
    decimal? South,
    decimal? East,
    decimal? West)
{
    public bool HasViewport => North is not null && South is not null && East is not null && West is not null;
}

public static class PlaceSearchGeographyPolicy
{
    public static PlaceSearchGeography Normalize(PlaceSearchRequest request)
    {
        var hasCenter = IsLatitude(request.CenterLatitude) && IsLongitude(request.CenterLongitude);
        var hasViewport = IsLatitude(request.North) && IsLatitude(request.South)
            && IsLongitude(request.East) && IsLongitude(request.West)
            && request.North > request.South && request.East > request.West;

        return new PlaceSearchGeography(
            hasCenter ? request.CenterLatitude : null,
            hasCenter ? request.CenterLongitude : null,
            hasViewport ? request.North : null,
            hasViewport ? request.South : null,
            hasViewport ? request.East : null,
            hasViewport ? request.West : null);
    }

    private static bool IsLatitude(decimal? value) => value is >= -90m and <= 90m;
    private static bool IsLongitude(decimal? value) => value is >= -180m and <= 180m;
}
