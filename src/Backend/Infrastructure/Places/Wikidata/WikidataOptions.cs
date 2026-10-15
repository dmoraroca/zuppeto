namespace Zuppeto.Infrastructure.Places.Wikidata;

public sealed class WikidataOptions
{
    public const string SectionName = "PlaceSources:Wikidata";
    public bool Enabled { get; init; } = true;
    public string BaseUrl { get; init; } = "https://www.wikidata.org/wiki/Special:EntityData/";
    public string CommonsApiBaseUrl { get; init; } = "https://commons.wikimedia.org/w/api.php";
    public int TimeoutSeconds { get; init; } = 15;
    public string? FixtureDirectory { get; init; }
}
