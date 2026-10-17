namespace Zuppeto.Infrastructure.Places.OpenStreetMap;

public sealed class OsmDirectMapOptions
{
    public const string SectionName = "OpenStreetMap:DirectMap";
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://api.openstreetmap.org/api/0.6/";
    public int TimeoutSeconds { get; set; } = 45;
    public decimal MaximumBoundingBoxArea { get; set; } = 0.01m;
    public long MaximumDownloadBytes { get; set; } = 25_000_000;
    public Guid TerritorialUnitId { get; set; } = Guid.Parse("8680aa5a-292f-4201-8d52-d6381f438af4");
    public string OfficialCode { get; set; } = "08006";
    public decimal West { get; set; } = 2.521202m;
    public decimal South { get; set; } = 41.5694737m;
    public decimal East { get; set; } = 2.5720886m;
    public decimal North { get; set; } = 41.5956628m;
}

internal sealed record OsmDirectMapMetrics(
    long DownloadBytes,
    TimeSpan DownloadDuration,
    TimeSpan ParseDuration,
    long ManagedMemoryDeltaBytes,
    int NodeCount,
    int WayCount,
    int RelationCount,
    int UsefulPoiCount);
