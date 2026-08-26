namespace Zuppeto.Infrastructure.GooglePlaces;

public sealed class GooglePlacesOptions
{
    public const string SectionName = "GooglePlaces";

    /// <summary>
    /// When false, Text Search / external place discovery is skipped (catalog/snapshots only).
    /// </summary>
    public bool Enabled { get; set; } = true;

    public string BaseUrl { get; set; } = "https://maps.googleapis.com/maps/api/place/";

    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 6;

    /// <summary>
    /// Advertised Google coordinate cache retention in days (same JSON keys as application integration options).
    /// </summary>
    public int CoordinateCacheRetentionDays { get; set; } = 30;
}
