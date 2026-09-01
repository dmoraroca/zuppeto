using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.GooglePlaces;

internal sealed class GooglePlacesSuggestionProvider(
    HttpClient httpClient,
    IOptions<GooglePlacesOptions> options,
    ILogger<GooglePlacesSuggestionProvider> logger)
    : IExternalPlaceSuggestionProvider, IExternalPlaceDetailsProvider
{
    private const string NewPlacesFieldMask =
        "id,displayName,formattedAddress,location,photos.name,photos.authorAttributions,rating,userRatingCount,priceLevel,allowsDogs,outdoorSeating,restroom,reservable,goodForChildren,takeout,editorialSummary,nationalPhoneNumber,websiteUri,regularOpeningHours.weekdayDescriptions,types";

    private static volatile bool newPlacesApiDisabled;

    private readonly GooglePlacesOptions googleOptions = options.Value;

    public async Task<IReadOnlyCollection<PlaceExternalCandidateDto>> SearchPlacesAsync(
        PlaceExternalSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetApiKey(requireDiscoveryEnabled: true, out var apiKey))
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
                    ResolveCity(item.FormattedAddress),
                    ResolveCountry(item.FormattedAddress),
                    item.Geometry?.Location?.Lat ?? 0,
                    item.Geometry?.Location?.Lng ?? 0,
                    item.PlaceId?.Trim() ?? string.Empty,
                    "google_places",
                    null,
                    item.Photos?.FirstOrDefault()?.PhotoReference?.Trim()))
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

    public async Task<PlaceExternalDetailsDto?> GetDetailsAsync(
        string googlePlaceId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetApiKey(requireDiscoveryEnabled: false, out var apiKey))
        {
            return null;
        }

        var placeId = googlePlaceId.Trim();
        if (placeId.Length == 0)
        {
            return null;
        }

        PlaceExternalDetailsDto? fromNew = null;
        if (!newPlacesApiDisabled)
        {
            fromNew = await TryGetDetailsFromNewApiAsync(placeId, apiKey, cancellationToken);
        }

        var fromLegacy = await TryGetDetailsFromLegacyApiAsync(placeId, apiKey, cancellationToken);
        if (fromLegacy is not null && HasPhoto(fromLegacy))
        {
            if (fromNew is null)
            {
                return fromLegacy;
            }

            return fromNew with
            {
                PhotoReference = fromLegacy.PhotoReference,
                PhotoAttribution = fromLegacy.PhotoAttribution ?? fromNew.PhotoAttribution,
                PhotoSourceUri = fromLegacy.PhotoSourceUri ?? fromNew.PhotoSourceUri,
                ExtraPhotoReferences = fromLegacy.ExtraPhotoReferences
            };
        }

        return fromNew ?? fromLegacy;
    }

    private static bool HasPhoto(PlaceExternalDetailsDto details) =>
        details.PhotoReferenceCandidates().Any();

    public async Task<byte[]?> DownloadPhotoAsync(
        string photoReferenceOrName,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetApiKey(requireDiscoveryEnabled: false, out var apiKey))
        {
            return null;
        }

        var reference = photoReferenceOrName.Trim();
        if (reference.Length == 0)
        {
            return null;
        }

        try
        {
            Uri requestUri = reference.StartsWith("places/", StringComparison.OrdinalIgnoreCase)
                ? new Uri($"https://places.googleapis.com/v1/{reference}/media?maxWidthPx=1600&key={Uri.EscapeDataString(apiKey)}")
                : new Uri($"photo?maxwidth=1600&photo_reference={Uri.EscapeDataString(reference)}&key={Uri.EscapeDataString(apiKey)}", UriKind.Relative);

            for (var hop = 0; hop < 6; hop++)
            {
                using var response = await httpClient.GetAsync(requestUri, cancellationToken);
                if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is { } location)
                {
                    requestUri = location.IsAbsoluteUri
                        ? location
                        : new Uri(httpClient.BaseAddress ?? new Uri("https://maps.googleapis.com/"), location);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "Google Place Photo returned {StatusCode} for reference {Reference}.",
                        (int)response.StatusCode,
                        reference);
                    return null;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                if (bytes.Length == 0 || !LooksLikeImage(bytes))
                {
                    logger.LogWarning(
                        "Google Place Photo payload was not an image ({Length} bytes) for reference {Reference}.",
                        bytes.Length,
                        reference);
                    return null;
                }

                return bytes;
            }

            logger.LogWarning("Google Place Photo exceeded redirect limit for reference {Reference}.", reference);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Google Place Photo download failed for reference {Reference}.", reference);
            return null;
        }
    }

    private async Task<PlaceExternalDetailsDto?> TryGetDetailsFromNewApiAsync(
        string placeId,
        string apiKey,
        CancellationToken cancellationToken)
    {
        try
        {
            using var message = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://places.googleapis.com/v1/places/{Uri.EscapeDataString(placeId)}?languageCode=ca");
            message.Headers.TryAddWithoutValidation("X-Goog-Api-Key", apiKey);
            message.Headers.TryAddWithoutValidation("X-Goog-FieldMask", NewPlacesFieldMask);

            using var response = await httpClient.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden
                    || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    newPlacesApiDisabled = true;
                    logger.LogWarning(
                        "Places API (New) returned {StatusCode}; using legacy Place Details for this process.",
                        (int)response.StatusCode);
                }
                else
                {
                    logger.LogDebug(
                        "Places API (New) details returned {StatusCode} for {PlaceId}.",
                        (int)response.StatusCode,
                        placeId);
                }

                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<GooglePlaceNewDetailsResponse>(cancellationToken);
            if (payload is null)
            {
                return null;
            }

            var photo = payload.Photos?.FirstOrDefault();
            var author = photo?.AuthorAttributions?.FirstOrDefault();
            var extraPhotos = ExtraPhotoNames(payload.Photos, photo?.Name);
            return new PlaceExternalDetailsDto(
                placeId,
                payload.DisplayName?.Text?.Trim(),
                payload.FormattedAddress?.Trim(),
                payload.Location?.Latitude,
                payload.Location?.Longitude,
                payload.Rating,
                payload.UserRatingCount,
                MapNewPriceLevel(payload.PriceLevel),
                payload.EditorialSummary?.Text?.Trim(),
                payload.AllowsDogs,
                payload.OutdoorSeating,
                payload.Reservable,
                payload.Takeout,
                payload.Restroom,
                payload.GoodForChildren,
                photo?.Name?.Trim(),
                author?.DisplayName?.Trim(),
                author?.Uri?.Trim(),
                payload.NationalPhoneNumber?.Trim(),
                payload.WebsiteUri?.Trim(),
                JoinHours(payload.RegularOpeningHours?.WeekdayDescriptions),
                payload.Types,
                extraPhotos);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Places API (New) details failed for {PlaceId}.", placeId);
            return null;
        }
    }

    private async Task<PlaceExternalDetailsDto?> TryGetDetailsFromLegacyApiAsync(
        string placeId,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var fields =
            "place_id,name,formatted_address,geometry,photos,rating,user_ratings_total,price_level,editorial_summary,opening_hours,formatted_phone_number,international_phone_number,website,types";
        var url =
            $"details/json?place_id={Uri.EscapeDataString(placeId)}&fields={Uri.EscapeDataString(fields)}&language=ca&key={Uri.EscapeDataString(apiKey)}";

        try
        {
            var payload = await httpClient.GetFromJsonAsync<GooglePlacesLegacyDetailsResponse>(url, cancellationToken);
            var status = payload?.Status?.Trim() ?? string.Empty;
            if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase) || payload?.Result is null)
            {
                logger.LogWarning(
                    "Google Places details returned {Status} for {PlaceId}: {ErrorMessage}",
                    status,
                    placeId,
                    payload?.ErrorMessage);
                return null;
            }

            var result = payload.Result;
            var photo = result.Photos?.FirstOrDefault();
            var extraPhotos = ExtraPhotoReferences(result.Photos, photo?.PhotoReference);
            return new PlaceExternalDetailsDto(
                placeId,
                result.Name?.Trim(),
                result.FormattedAddress?.Trim(),
                result.Geometry?.Location?.Lat,
                result.Geometry?.Location?.Lng,
                result.Rating,
                result.UserRatingsTotal,
                MapLegacyPriceLevel(result.PriceLevel),
                result.EditorialSummary?.Overview?.Trim(),
                AllowsDogs: null,
                OutdoorSeating: null,
                Reservable: null,
                Takeout: null,
                Restroom: null,
                GoodForChildren: null,
                photo?.PhotoReference?.Trim(),
                photo?.HtmlAttributions?.FirstOrDefault()?.Trim(),
                PhotoSourceUri: null,
                result.FormattedPhoneNumber?.Trim() ?? result.InternationalPhoneNumber?.Trim(),
                result.Website?.Trim(),
                JoinHours(result.OpeningHours?.WeekdayText),
                result.Types,
                extraPhotos);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Google Places details failed for {PlaceId}.", placeId);
            return null;
        }
    }

    private bool TryGetApiKey(bool requireDiscoveryEnabled, out string apiKey)
    {
        apiKey = googleOptions.ApiKey?.Trim() ?? string.Empty;
        if (requireDiscoveryEnabled && !googleOptions.Enabled)
        {
            logger.LogDebug("Google Places Text Search skipped: Enabled is false.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Google Places call skipped: ApiKey is empty.");
            return false;
        }

        return true;
    }

    private static bool LooksLikeImage(byte[] bytes)
    {
        if (bytes.Length < 12)
        {
            return false;
        }

        return bytes[0] == 0xFF && bytes[1] == 0xD8
            || bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47
            || bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46
            || bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46;
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

    private static string ResolveCity(string? formattedAddress)
    {
        var address = formattedAddress?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(address))
        {
            return string.Empty;
        }

        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[^2] : parts[0];
    }

    private static string ResolveCountry(string? formattedAddress)
    {
        var address = formattedAddress?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(address))
        {
            return string.Empty;
        }

        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : string.Empty;
    }

    private static string? JoinHours(IEnumerable<string>? lines)
    {
        var parts = (lines ?? [])
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToArray();
        return parts.Length == 0 ? null : string.Join("\n", parts);
    }

    private static IReadOnlyList<string>? ExtraPhotoNames(List<GooglePlaceNewPhoto>? photos, string? first)
    {
        var extra = (photos ?? [])
            .Select(photo => photo.Name?.Trim() ?? string.Empty)
            .Where(name => name.Length > 0 && !string.Equals(name, first?.Trim(), StringComparison.Ordinal))
            .Take(4)
            .ToArray();
        return extra.Length == 0 ? null : extra;
    }

    private static IReadOnlyList<string>? ExtraPhotoReferences(List<GooglePlacesPhoto>? photos, string? first)
    {
        var extra = (photos ?? [])
            .Select(photo => photo.PhotoReference?.Trim() ?? string.Empty)
            .Where(reference =>
                reference.Length > 0
                && !string.Equals(reference, first?.Trim(), StringComparison.Ordinal))
            .Take(4)
            .ToArray();
        return extra.Length == 0 ? null : extra;
    }

    private static string? MapNewPriceLevel(string? priceLevel)
    {
        return priceLevel?.Trim().ToUpperInvariant() switch
        {
            "PRICE_LEVEL_FREE" => "Gratuït",
            "PRICE_LEVEL_INEXPENSIVE" => "€",
            "PRICE_LEVEL_MODERATE" => "€€",
            "PRICE_LEVEL_EXPENSIVE" => "€€€",
            "PRICE_LEVEL_VERY_EXPENSIVE" => "€€€€",
            _ => null
        };
    }

    private static string? MapLegacyPriceLevel(int? priceLevel)
    {
        return priceLevel switch
        {
            0 => "Gratuït",
            1 => "€",
            2 => "€€",
            3 => "€€€",
            4 => "€€€€",
            _ => null
        };
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

        [JsonPropertyName("photos")]
        public List<GooglePlacesPhoto>? Photos { get; init; }
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

    private sealed class GooglePlacesPhoto
    {
        [JsonPropertyName("photo_reference")]
        public string? PhotoReference { get; init; }

        [JsonPropertyName("html_attributions")]
        public List<string>? HtmlAttributions { get; init; }
    }

    private sealed class GooglePlacesLegacyDetailsResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; init; }

        [JsonPropertyName("result")]
        public GooglePlacesLegacyDetailsResult? Result { get; init; }
    }

    private sealed class GooglePlacesLegacyDetailsResult
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; init; }

        [JsonPropertyName("geometry")]
        public GooglePlacesGeometry? Geometry { get; init; }

        [JsonPropertyName("photos")]
        public List<GooglePlacesPhoto>? Photos { get; init; }

        [JsonPropertyName("rating")]
        public decimal? Rating { get; init; }

        [JsonPropertyName("user_ratings_total")]
        public int? UserRatingsTotal { get; init; }

        [JsonPropertyName("price_level")]
        public int? PriceLevel { get; init; }

        [JsonPropertyName("editorial_summary")]
        public GooglePlacesEditorialSummary? EditorialSummary { get; init; }

        [JsonPropertyName("opening_hours")]
        public GooglePlacesOpeningHours? OpeningHours { get; init; }

        [JsonPropertyName("formatted_phone_number")]
        public string? FormattedPhoneNumber { get; init; }

        [JsonPropertyName("international_phone_number")]
        public string? InternationalPhoneNumber { get; init; }

        [JsonPropertyName("website")]
        public string? Website { get; init; }

        [JsonPropertyName("types")]
        public List<string>? Types { get; init; }
    }

    private sealed class GooglePlacesOpeningHours
    {
        [JsonPropertyName("weekday_text")]
        public List<string>? WeekdayText { get; init; }
    }

    private sealed class GooglePlacesEditorialSummary
    {
        [JsonPropertyName("overview")]
        public string? Overview { get; init; }
    }

    private sealed class GooglePlaceNewDetailsResponse
    {
        [JsonPropertyName("displayName")]
        public GooglePlaceLocalizedText? DisplayName { get; init; }

        [JsonPropertyName("formattedAddress")]
        public string? FormattedAddress { get; init; }

        [JsonPropertyName("location")]
        public GooglePlaceLatLng? Location { get; init; }

        [JsonPropertyName("photos")]
        public List<GooglePlaceNewPhoto>? Photos { get; init; }

        [JsonPropertyName("rating")]
        public decimal? Rating { get; init; }

        [JsonPropertyName("userRatingCount")]
        public int? UserRatingCount { get; init; }

        [JsonPropertyName("priceLevel")]
        public string? PriceLevel { get; init; }

        [JsonPropertyName("editorialSummary")]
        public GooglePlaceLocalizedText? EditorialSummary { get; init; }

        [JsonPropertyName("allowsDogs")]
        public bool? AllowsDogs { get; init; }

        [JsonPropertyName("outdoorSeating")]
        public bool? OutdoorSeating { get; init; }

        [JsonPropertyName("reservable")]
        public bool? Reservable { get; init; }

        [JsonPropertyName("takeout")]
        public bool? Takeout { get; init; }

        [JsonPropertyName("restroom")]
        public bool? Restroom { get; init; }

        [JsonPropertyName("goodForChildren")]
        public bool? GoodForChildren { get; init; }

        [JsonPropertyName("nationalPhoneNumber")]
        public string? NationalPhoneNumber { get; init; }

        [JsonPropertyName("websiteUri")]
        public string? WebsiteUri { get; init; }

        [JsonPropertyName("regularOpeningHours")]
        public GooglePlaceRegularOpeningHours? RegularOpeningHours { get; init; }

        [JsonPropertyName("types")]
        public List<string>? Types { get; init; }
    }

    private sealed class GooglePlaceRegularOpeningHours
    {
        [JsonPropertyName("weekdayDescriptions")]
        public List<string>? WeekdayDescriptions { get; init; }
    }

    private sealed class GooglePlaceLocalizedText
    {
        [JsonPropertyName("text")]
        public string? Text { get; init; }
    }

    private sealed class GooglePlaceLatLng
    {
        [JsonPropertyName("latitude")]
        public decimal Latitude { get; init; }

        [JsonPropertyName("longitude")]
        public decimal Longitude { get; init; }
    }

    private sealed class GooglePlaceNewPhoto
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("authorAttributions")]
        public List<GooglePlaceAuthorAttribution>? AuthorAttributions { get; init; }
    }

    private sealed class GooglePlaceAuthorAttribution
    {
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; init; }

        [JsonPropertyName("uri")]
        public string? Uri { get; init; }
    }
}
