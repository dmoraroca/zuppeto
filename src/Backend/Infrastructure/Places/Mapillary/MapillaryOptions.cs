using Microsoft.Extensions.Options;

namespace Zuppeto.Infrastructure.Places.Mapillary;

public sealed class MapillaryOptions
{
    public const string SectionName = "Mapillary";

    public bool Enabled { get; init; }
    public string? AccessToken { get; init; }
    public string BaseUrl { get; init; } = "https://graph.mapillary.com/";
    public int TimeoutSeconds { get; init; } = 15;
}

internal sealed class MapillaryOptionsValidator : IValidateOptions<MapillaryOptions>
{
    public ValidateOptionsResult Validate(string? name, MapillaryOptions options)
    {
        if (!options.Enabled) return ValidateOptionsResult.Success;

        if (string.IsNullOrWhiteSpace(options.AccessToken))
        {
            return ValidateOptionsResult.Fail(
                "Mapillary està habilitat però falta Mapillary:AccessToken. " +
                "Configura'l amb .NET User Secrets o amb la variable d'entorn Mapillary__AccessToken.");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
            return ValidateOptionsResult.Fail("Mapillary:BaseUrl ha de ser una URL HTTPS absoluta.");

        if (options.TimeoutSeconds is < 5 or > 60)
            return ValidateOptionsResult.Fail("Mapillary:TimeoutSeconds ha d'estar entre 5 i 60 segons.");

        return ValidateOptionsResult.Success;
    }
}
