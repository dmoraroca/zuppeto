namespace Zuppeto.Infrastructure.Places.PublicAdministration;

public sealed class CataloniaTourismRegistryOptions
{
    public const string SectionName = "PlaceSources:CataloniaTourismRegistry";
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://analisi.transparenciacatalunya.cat/";
    public string DatasetId { get; set; } = "t2h3-cgys";
    public int TimeoutSeconds { get; set; } = 30;
    public string? FixturePath { get; set; }
}
