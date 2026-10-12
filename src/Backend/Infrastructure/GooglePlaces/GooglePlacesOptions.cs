namespace Zuppeto.Infrastructure.GooglePlaces;

public sealed class GooglePlacesOptions
{
    public const string SectionName = "GooglePlaces";

    /// <summary>
    /// Operational kill switch. It is intentionally not configurable so an old environment
    /// variable or deployment secret cannot reactivate Google Places accidentally.
    /// </summary>
    public static bool RuntimeCallsDisabled => true;

    /// <summary>
    /// Retained for the Phase I audit. Runtime calls remain blocked by
    /// <see cref="RuntimeCallsDisabled"/> even when this value is overridden.
    /// </summary>
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "https://maps.googleapis.com/maps/api/place/";

    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 6;

    /// <summary>
    /// Advertised Google coordinate cache retention in days (same JSON keys as application integration options).
    /// </summary>
    public int CoordinateCacheRetentionDays { get; set; } = 30;

    /// <summary>
    /// Activa Places API (New). Es manté desactivada mentre la clau de desenvolupament
    /// només tingui autoritzada Places API Legacy.
    /// </summary>
    public bool UseNewApi { get; set; }
}
