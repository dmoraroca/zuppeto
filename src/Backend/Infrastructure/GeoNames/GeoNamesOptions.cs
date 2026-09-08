namespace Zuppeto.Infrastructure.GeoNames;

public sealed class GeoNamesOptions
{
    public const string SectionName = "GeoNames";

    public string BaseUrl { get; set; } = "https://secure.geonames.org/";

    public string Username { get; set; } = string.Empty;

    public int DefaultMaxRows { get; set; } = 1000;

    public int TimeoutSeconds { get; set; } = 6;

    public int CacheMinutes { get; set; } = 15;
}
