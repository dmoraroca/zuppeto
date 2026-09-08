using System.Text.Json.Serialization;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.GeoNames;

internal sealed class GeoNamesCitySuggestionProvider(
    HttpClient httpClient,
    IOptions<GeoNamesOptions> options,
    IMemoryCache cache,
    ILogger<GeoNamesCitySuggestionProvider> logger) : IExternalCitySuggestionProvider
{
    private readonly GeoNamesOptions geoOptions = options.Value;

    public async Task<IReadOnlyCollection<PlaceCitySuggestionDto>> SearchCitiesAsync(
        string normalizedQuery,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0 || string.IsNullOrWhiteSpace(geoOptions.Username))
        {
            return [];
        }

        var effectiveLimit = Math.Min(Math.Max(limit, 1), Math.Min(1000, Math.Max(1, geoOptions.DefaultMaxRows)));
        var cacheKey = string.IsNullOrWhiteSpace(normalizedQuery)
            ? $"geonames:cities:eu:top:{effectiveLimit}"
            : $"geonames:cities:eu:{normalizedQuery.ToLowerInvariant()}:{effectiveLimit}";

        if (cache.TryGetValue(cacheKey, out IReadOnlyCollection<PlaceCitySuggestionDto>? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            var nameFilter = string.IsNullOrWhiteSpace(normalizedQuery)
                ? string.Empty
                : $"name_startsWith={Uri.EscapeDataString(normalizedQuery)}&";
            var uri =
                $"searchJSON?{nameFilter}featureClass=P&continentCode=EU&orderby=population&style=FULL&maxRows={effectiveLimit}" +
                $"&lang=ca&username={Uri.EscapeDataString(geoOptions.Username)}";

            var payload = await httpClient.GetFromJsonAsync<GeoNamesSearchResponse>(uri, cancellationToken);
            var cities = payload?.Geonames?
                .Where(item => !string.IsNullOrWhiteSpace(item.Name))
                .Select(item =>
                {
                    var city = item.Name!.Trim();
                    var country = item.CountryName?.Trim() ?? string.Empty;
                    var countryCode = item.CountryCode?.Trim();
                    var region = item.AdminName1?.Trim();
                    var countryWithRegion = BuildCountryWithRegion(country, countryCode, region);
                    return new PlaceCitySuggestionDto(
                        city,
                        country,
                        countryCode,
                        PlaceCitySuggestionFormatter.BuildDisplayLabel(city, countryWithRegion),
                        "geonames");
                })
                .GroupBy(
                    item => $"{item.City.Trim().ToLowerInvariant()}|{item.Country.Trim().ToLowerInvariant()}",
                    StringComparer.Ordinal)
                .Select(group => group.First())
                .Take(effectiveLimit)
                .ToArray() ?? [];

            cache.Set(cacheKey, cities, TimeSpan.FromMinutes(Math.Max(1, geoOptions.CacheMinutes)));
            return cities;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("GeoNames city suggestion timed out for query {Query}.", normalizedQuery);
            return [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GeoNames city suggestion failed for query {Query}.", normalizedQuery);
            return [];
        }
    }

    private sealed class GeoNamesSearchResponse
    {
        [JsonPropertyName("geonames")]
        public List<GeoNamesCityItem>? Geonames { get; init; }
    }

    private sealed class GeoNamesCityItem
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("countryCode")]
        public string? CountryCode { get; init; }

        [JsonPropertyName("countryName")]
        public string? CountryName { get; init; }

        [JsonPropertyName("adminName1")]
        public string? AdminName1 { get; init; }
    }

    private static string BuildCountryWithRegion(string country, string? countryCode, string? region)
    {
        if (!string.Equals(countryCode, "ES", StringComparison.OrdinalIgnoreCase))
        {
            return country;
        }

        if (string.IsNullOrWhiteSpace(region))
        {
            return country;
        }

        return $"{region.Trim()}, {country}";
    }
}
