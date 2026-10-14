namespace Zuppeto.Infrastructure.Places.OpenStreetMap;

public sealed class OverpassOptions
{
    public const string SectionName = "OpenStreetMap:Overpass";
    public string BaseUrl { get; set; } = "https://overpass-api.de/api/interpreter";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaximumRetries { get; set; } = 2;
    public int BackoffMilliseconds { get; set; } = 750;
    public int MaximumItems { get; set; } = 250;
    public string? FixturePath { get; set; }
}
