namespace Zuppeto.Infrastructure.Places.OpenStreetMap;

public sealed class GeofabrikExtractOptions
{
    public const string SectionName = "OpenStreetMap:GeofabrikExtract";
    public string SourceUrl { get; set; } = "https://download.geofabrik.de/europe/spain/cataluna-latest.osm.pbf";
    public string? ExtractPath { get; set; }
    public string? SourceVersion { get; set; }
    /// <summary>
    /// Límit de POI retornats per municipi. Un valor igual o inferior a zero desactiva
    /// el truncament per als extractes locals; les fonts HTTP mantenen els seus propis límits.
    /// </summary>
    public int MaximumItems { get; set; }
    public bool ReusableIndexEnabled { get; set; } = true;
    public string IndexDirectory { get; set; } = "/tmp/petiloc-osm-index";
}
