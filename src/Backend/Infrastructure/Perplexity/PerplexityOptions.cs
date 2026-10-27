using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Zuppeto.Infrastructure.Perplexity;

public sealed class PerplexityOptions
{
    public const string SectionName = "Perplexity";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://api.perplexity.ai/";
    public int TimeoutSeconds { get; set; } = 30;

    internal string? ApiKey { get; set; }
    internal string? ConfigurationError { get; set; }
}

internal sealed class PerplexityOptionsSetup(IConfiguration configuration)
    : IConfigureOptions<PerplexityOptions>
{
    private const string DirectEnvironmentKey = "PERPLEXITY_API_KEY";
    private const string FileEnvironmentKey = "PERPLEXITY_API_KEY_FILE";

    public void Configure(PerplexityOptions options)
    {
        var section = configuration.GetSection(PerplexityOptions.SectionName);
        section.Bind(options);

        if (!options.Enabled)
        {
            options.ApiKey = null;
            return;
        }

        var userSecretValue = section["ApiKey"];
        var environmentValue = configuration[DirectEnvironmentKey];
        var secretFile = configuration[FileEnvironmentKey] ?? section["ApiKeyFile"];
        var directSourceCount = new[] { userSecretValue, environmentValue }
            .Count(value => !string.IsNullOrWhiteSpace(value));

        if (directSourceCount > 1 ||
            (directSourceCount > 0 && !string.IsNullOrWhiteSpace(secretFile)))
        {
            options.ConfigurationError =
                "Configura exactament una font per a la credencial de Perplexity.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(secretFile))
        {
            try
            {
                options.ApiKey = File.ReadAllText(secretFile).Trim();
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                options.ConfigurationError =
                    "No s'ha pogut llegir el secret de Perplexity des del fitxer configurat.";
            }

            return;
        }

        options.ApiKey = string.IsNullOrWhiteSpace(environmentValue)
            ? userSecretValue?.Trim()
            : environmentValue.Trim();
    }
}

internal sealed class PerplexityOptionsValidator : IValidateOptions<PerplexityOptions>
{
    public ValidateOptionsResult Validate(string? name, PerplexityOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (!string.IsNullOrWhiteSpace(options.ConfigurationError))
        {
            return ValidateOptionsResult.Fail(options.ConfigurationError);
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return ValidateOptionsResult.Fail(
                "Perplexity està habilitat però no té cap credencial configurada.");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
        {
            return ValidateOptionsResult.Fail(
                "Perplexity:BaseUrl ha de ser una URL HTTPS absoluta.");
        }

        if (options.TimeoutSeconds is < 5 or > 120)
        {
            return ValidateOptionsResult.Fail(
                "Perplexity:TimeoutSeconds ha d'estar entre 5 i 120 segons.");
        }

        return ValidateOptionsResult.Success;
    }
}
