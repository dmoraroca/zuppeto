using System.Text.Json.Serialization;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using YepPet.Application.Places;

namespace YepPet.Infrastructure.GooglePlaces;

internal sealed class GooglePlacesSuggestionProvider(
    HttpClient httpClient,
    IOptions<GooglePlacesOptions> options) : IExternalPlaceSuggestionProvider
{
    private readonly GooglePlacesOptions googleOptions = options.Value;

    public async Task<IReadOnlyCollection<PlaceExternalCandidateDto>> SearchPlacesAsync(
        PlaceExternalSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var apiKey = googleOptions.ApiKey?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
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
        catch
        {
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
