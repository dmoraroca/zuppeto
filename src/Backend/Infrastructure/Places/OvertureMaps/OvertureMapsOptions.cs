namespace Zuppeto.Infrastructure.Places.OvertureMaps;

public sealed class OvertureMapsOptions
{
    public const string SectionName = "PlaceSources:OvertureMaps";

    public bool Enabled { get; set; }
    public string ReleaseVersion { get; set; } = "2026-09-23.1";
    public string ExtractPath { get; set; } = string.Empty;
    public string BoundaryGeoJsonPath { get; set; } = string.Empty;
    public bool RequireVerifiedBoundary { get; set; } = true;

    internal string ResolveExtractPath(string countryIso2, string officialCode) =>
        ResolveTemplate(ExtractPath, countryIso2, officialCode);

    internal string ResolveBoundaryPath(string countryIso2, string officialCode) =>
        ResolveTemplate(BoundaryGeoJsonPath, countryIso2, officialCode);

    private static string ResolveTemplate(string template, string countryIso2, string officialCode) => template
        .Replace("{countryIso2}", countryIso2.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)
        .Replace("{officialCode}", officialCode, StringComparison.OrdinalIgnoreCase);
}
