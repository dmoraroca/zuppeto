namespace Zuppeto.Infrastructure.Places.OpenStreetMap;

public sealed class GeofabrikExtractOptions
{
    public const string SectionName = "OpenStreetMap:GeofabrikExtract";
    public string SourceUrl { get; set; } = "https://download.geofabrik.de/europe/spain/cataluna-latest.osm.pbf";
    public string? ExtractPath { get; set; }
    public string? SourceVersion { get; set; }
    public int MaximumItems { get; set; } = 250;
}
