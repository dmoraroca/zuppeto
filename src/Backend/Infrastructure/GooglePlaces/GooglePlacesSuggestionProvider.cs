using System.Text.Json.Serialization;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.GooglePlaces;

internal sealed class GooglePlacesSuggestionProvider(
    HttpClient httpClient,
    IOptions<GooglePlacesOptions> options,
    ILogger<GooglePlacesSuggestionProvider> logger) : IExternalPlaceSuggestionProvider
{
    private readonly GooglePlacesOptions googleOptions = options.Value;

    public async Task<IReadOnlyCollection<PlaceExternalCandidateDto>> SearchPlacesAsync(
        PlaceExternalSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var apiKey = googleOptions.ApiKey?.Trim() ?? string.Empty;
        if (!googleOptions.Enabled)
        {
            logger.LogDebug("Google Places search skipped: Enabled is false.");
            return [];
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Google Places search skipped: ApiKey is empty.");
            return [];
        }

        var query = BuildQuery(request);
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var limit = Math.Clamp(request.Limit ?? 10, 1, 20);
        var url =
            $"textsearch/json?query={Uri.EscapeDataString(query)}&region=eu&language=ca&key={Uri.EscapeDataString(apiKey)}";

        try
        {
            var payload = await httpClient.GetFromJsonAsync<GooglePlacesSearchResponse>(url, cancellationToken);
            var status = payload?.Status?.Trim() ?? string.Empty;
            if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(status, "ZERO_RESULTS", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "Google Places text search returned {Status} for query {Query}: {ErrorMessage}",
                    status,
                    query,
                    payload?.ErrorMessage);
                return [];
            }

            var candidates = payload?.Results ?? [];
            return candidates
                .Where(item => !string.IsNullOrWhiteSpace(item.Name))
                .Select(item => new PlaceExternalCandidateDto(
                    item.Name?.Trim() ?? string.Empty,
                    item.FormattedAddress?.Trim() ?? string.Empty,
                    ResolveCity(item),
                    ResolveCountry(item),
                    item.Geometry?.Location?.Lat ?? 0,
                    item.Geometry?.Location?.Lng ?? 0,
                    item.PlaceId?.Trim() ?? string.Empty,
                    "google_places",
                    null))
                .Where(item => item.Name.Length > 0 && item.ExternalId.Length > 0)
                .Take(limit)
                .ToArray();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Google Places text search failed for query {Query}.", query);
            return [];
        }
    }

    private static string BuildQuery(PlaceExternalSearchRequest request)
    {
        var text = request.Query?.Trim() ?? string.Empty;
        var city = request.City?.Trim() ?? string.Empty;
        var type = request.Type?.Trim() ?? string.Empty;
        var parts = new[] { text, type, city, "pet friendly" }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();
        return string.Join(", ", parts);
    }

    private static string ResolveCity(GooglePlacesResult item)
    {
        var address = item.FormattedAddress?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(address))
        {
            return string.Empty;
        }

        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[^2] : parts[0];
    }

    private static string ResolveCountry(GooglePlacesResult item)
    {
        var address = item.FormattedAddress?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(address))
        {
            return string.Empty;
        }

        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : string.Empty;
    }

    private sealed class GooglePlacesSearchResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; init; }

        [JsonPropertyName("results")]
        public List<GooglePlacesResult>? Results { get; init; }
    }

    private sealed class GooglePlacesResult
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; init; }

        [JsonPropertyName("place_id")]
        public string? PlaceId { get; init; }

        [JsonPropertyName("geometry")]
        public GooglePlacesGeometry? Geometry { get; init; }
    }

    private sealed class GooglePlacesGeometry
    {
        [JsonPropertyName("location")]
        public GooglePlacesLocation? Location { get; init; }
    }

    private sealed class GooglePlacesLocation
    {
        [JsonPropertyName("lat")]
        public decimal Lat { get; init; }

        [JsonPropertyName("lng")]
        public decimal Lng { get; init; }
    }
}
