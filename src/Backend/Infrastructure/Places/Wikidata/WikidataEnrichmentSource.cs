using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.Wikidata;

internal sealed class WikidataEnrichmentSource(HttpClient http, IOptions<WikidataOptions> options) : IPlaceEnrichmentSource
{
    private const string License = "CC0-1.0";
    private const string Attribution = "Dades de Wikidata";
    private readonly WikidataOptions settings = options.Value;

    public PlaceSourceDescriptor Descriptor { get; } = new(
        WikidataSource.SourceKey,
        WikidataSource.EntityDataAccessMode,
        PlaceSourceCapability.Enrichment | PlaceSourceCapability.ProvenanceVerification | PlaceSourceCapability.StoragePermissionValidation,
        ["Només enriqueix identificadors Wikidata explícits", "No importa textos, fitxers ni imatges enllaçats"],
        License,
        Attribution,
        true,
        new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero));

    public async Task<Result<PlaceSourceDocument>> EnrichAsync(string externalId, CancellationToken cancellationToken)
    {
        var id = externalId.Trim().ToUpperInvariant();
        if (!settings.Enabled) return Result<PlaceSourceDocument>.Fail(FailureKind.ExternalUnavailable, "Wikidata està desactivat.");
        if (!IsEntityId(id)) return Result<PlaceSourceDocument>.Fail(FailureKind.Invalid, "L’identificador Wikidata no és vàlid.");

        if (!string.IsNullOrWhiteSpace(settings.FixtureDirectory))
        {
            var fixturePath = Path.Combine(settings.FixtureDirectory, $"{id}.json");
            if (!File.Exists(fixturePath)) return Result<PlaceSourceDocument>.Fail(FailureKind.NotFound, "La fixture de Wikidata no conté l’entitat indicada.");
            await using var fixture = File.OpenRead(fixturePath);
            return await ParseAsync(fixture, id, cancellationToken);
        }

        using var response = await http.GetAsync($"{id}.json?flavor=simple", cancellationToken);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            return Result<PlaceSourceDocument>.Fail(FailureKind.RateLimited, "Wikidata ha limitat temporalment l’enriquiment.");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result<PlaceSourceDocument>.Fail(FailureKind.NotFound, "Wikidata no conté l’entitat indicada.");
        if (!response.IsSuccessStatusCode)
            return Result<PlaceSourceDocument>.Fail(FailureKind.ExternalUnavailable, $"Wikidata ha respost HTTP {(int)response.StatusCode}.");

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await ParseAsync(stream, id, cancellationToken);
    }

    private static async Task<Result<PlaceSourceDocument>> ParseAsync(Stream stream, string id, CancellationToken cancellationToken)
    {
        try
        {
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!json.RootElement.GetProperty("entities").TryGetProperty(id, out var entity))
                return Result<PlaceSourceDocument>.Fail(FailureKind.NotFound, "Wikidata no ha retornat l’entitat indicada.");

            var observedAt = DateTimeOffset.UtcNow;
            var fields = new Dictionary<string, SourceFieldValue>(StringComparer.OrdinalIgnoreCase);
            Add(fields, "wikidata.id", id, observedAt);
            Add(fields, "name", Localized(entity, "labels"), observedAt);
            Add(fields, "description", Localized(entity, "descriptions"), observedAt);
            Add(fields, "website", StringClaim(entity, "P856"), observedAt);
            Add(fields, "brand.wikidata", EntityClaim(entity, "P1716"), observedAt);
            Add(fields, "operator.wikidata", EntityClaim(entity, "P137"), observedAt);
            Add(fields, "parentOrganization.wikidata", EntityClaim(entity, "P749"), observedAt);
            Add(fields, "officialName", MonolingualTextClaim(entity, "P1448"), observedAt);
            return Result<PlaceSourceDocument>.Success(new(WikidataSource.SourceKey, id,
                entity.TryGetProperty("lastrevid", out var revision) ? revision.GetInt64().ToString() : null,
                observedAt, new Uri($"https://www.wikidata.org/entity/{id}"), fields));
        }
        catch (JsonException)
        {
            return Result<PlaceSourceDocument>.Fail(FailureKind.ExternalUnavailable, "Wikidata ha retornat JSON no vàlid.");
        }
    }

    internal static bool IsEntityId(string value) => value.Length is >= 2 and <= 20
        && value[0] == 'Q' && value[1..].All(char.IsAsciiDigit);

    private static string? Localized(JsonElement entity, string property)
    {
        if (!entity.TryGetProperty(property, out var values)) return null;
        foreach (var language in new[] { "ca", "es", "en" })
            if (values.TryGetProperty(language, out var value) && value.TryGetProperty("value", out var text)) return text.GetString();
        return values.EnumerateObject().Select(x => x.Value.TryGetProperty("value", out var text) ? text.GetString() : null)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
    }

    private static string? StringClaim(JsonElement entity, string property) => ClaimValue(entity, property, value =>
        value.ValueKind == JsonValueKind.String ? value.GetString() : null);

    private static string? EntityClaim(JsonElement entity, string property) => ClaimValue(entity, property, value =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty("id", out var id) ? id.GetString() : null);

    private static string? MonolingualTextClaim(JsonElement entity, string property) => ClaimValue(entity, property, value =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty("text", out var text) ? text.GetString() : null);

    private static string? ClaimValue(JsonElement entity, string property, Func<JsonElement, string?> read)
    {
        if (!entity.TryGetProperty("claims", out var claims) || !claims.TryGetProperty(property, out var statements)) return null;
        foreach (var statement in statements.EnumerateArray())
        {
            if (statement.TryGetProperty("rank", out var rank) && rank.GetString() == "deprecated") continue;
            if (!statement.TryGetProperty("mainsnak", out var snak) || !snak.TryGetProperty("datavalue", out var data)
                || !data.TryGetProperty("value", out var value)) continue;
            var result = read(value); if (!string.IsNullOrWhiteSpace(result)) return result;
        }
        return null;
    }

    private static void Add(IDictionary<string, SourceFieldValue> fields, string key, string? value, DateTimeOffset observedAt)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        fields[key] = new(value, value, License, Attribution, true, observedAt, null, observedAt.AddYears(1));
    }
}
