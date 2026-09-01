namespace Zuppeto.Application.Places;

/// <summary>
/// Application-level settings for Google Places integration (reads the same configuration section as Infrastructure).
/// </summary>
public sealed class GooglePlacesIntegrationOptions
{
    public const string SectionName = "GooglePlaces";

    /// <summary>
    /// When false, place search never calls Google Places Text Search (catalog and search snapshots only).
    /// Missing covers still use Place Details / Place Photos when an API key is configured.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Days until advertised Google coordinate cache expiry. Aligns with operational refresh via Places Details using <c>place_id</c>.
    /// </summary>
    public int CoordinateCacheRetentionDays { get; set; } = 30;

    /// <summary>
    /// When true and the request has discovery text, call Google Places before the internal catalog (useful for local testing).
    /// </summary>
    public bool PreferExternalSearchFirst { get; set; }
}
