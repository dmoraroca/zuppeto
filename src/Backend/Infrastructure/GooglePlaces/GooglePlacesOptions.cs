namespace YepPet.Infrastructure.GooglePlaces;

public sealed class GooglePlacesOptions
{
    public const string SectionName = "GooglePlaces";

    public string BaseUrl { get; set; } = "https://maps.googleapis.com/maps/api/place/";

    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 6;
}
