namespace Zuppeto.Infrastructure.Places.PublicAdministration;

public sealed class AragonCommercialRegistryOptions
{
    public const string SectionName = "PlaceSources:AragonCommercialRegistry";
    public bool Enabled { get; set; }
    public string DownloadUrl { get; set; } =
        "https://opendata.aragon.es/GA_OD_Core/download?resource_id=324&formato=csv";
    public int TimeoutSeconds { get; set; } = 120;
    public string? FixturePath { get; set; }
}
