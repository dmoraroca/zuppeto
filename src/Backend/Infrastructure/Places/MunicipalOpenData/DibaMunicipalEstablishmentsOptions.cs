namespace Zuppeto.Infrastructure.Places.MunicipalOpenData;

public sealed class DibaMunicipalEstablishmentsOptions
{
    public const string SectionName = "PlaceSources:DibaMunicipalEstablishments";
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://do.diba.cat/api/";
    public int TimeoutSeconds { get; set; } = 30;
    public string? FixturePath { get; set; }
}
