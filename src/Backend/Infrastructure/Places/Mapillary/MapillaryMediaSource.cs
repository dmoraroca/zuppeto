using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.Mapillary;

internal sealed class MapillaryMediaSource(HttpClient http, IOptions<MapillaryOptions> options) : IPlaceProximityMediaSource
{
    private readonly MapillaryOptions settings = options.Value;

    public PlaceSourceDescriptor Descriptor { get; } = new(
        MapillarySource.SourceKey,
        MapillarySource.ProximityAccessMode,
        PlaceSourceCapability.Media | PlaceSourceCapability.ProvenanceVerification,
        ["Cerca imatges pròximes: la correspondència amb el local requereix revisió ADMIN", "No desa ni publica candidats automàticament"],
        "MAPILLARY_TERMS",
        "© Mapillary i els seus contribuïdors",
        false,
        new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero));

    public async Task<Result<IReadOnlyCollection<PlaceProximityMediaCandidate>>> SearchAsync(
        PlaceProximityMediaSearch search,
        CancellationToken cancellationToken)
    {
        if (!settings.Enabled)
            return Result<IReadOnlyCollection<PlaceProximityMediaCandidate>>.Fail(
                FailureKind.ExternalUnavailable, "Mapillary està desactivat.");
        if (string.IsNullOrWhiteSpace(settings.AccessToken))
            return Result<IReadOnlyCollection<PlaceProximityMediaCandidate>>.Fail(
                FailureKind.ExternalUnavailable, "Falta la configuració segura de Mapillary.");
        if (search.Latitude is < -90 or > 90 || search.Longitude is < -180 or > 180)
            return Result<IReadOnlyCollection<PlaceProximityMediaCandidate>>.Fail(
                FailureKind.Invalid, "Les coordenades de cerca de Mapillary no són vàlides.");

        var radius = Math.Clamp(search.RadiusMeters, 10, 250);
        var limit = Math.Clamp(search.Limit, 1, 25);
        var requestUri = BuildRequestUri(search.Latitude, search.Longitude, radius, Math.Max(100, limit * 10));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", settings.AccessToken.Trim());
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result<IReadOnlyCollection<PlaceProximityMediaCandidate>>.Fail(
                    FailureKind.ExternalUnavailable,
                    $"Mapillary ha retornat HTTP {(int)response.StatusCode}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return Result<IReadOnlyCollection<PlaceProximityMediaCandidate>>.Success(
                await ReadCandidatesAsync(stream, search.Latitude, search.Longitude, radius, limit, cancellationToken));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyCollection<PlaceProximityMediaCandidate>>.Fail(
                FailureKind.ExternalUnavailable, "La consulta a Mapillary ha superat el temps d’espera.");
        }
        catch (HttpRequestException)
        {
            return Result<IReadOnlyCollection<PlaceProximityMediaCandidate>>.Fail(
                FailureKind.ExternalUnavailable, "Mapillary no està disponible temporalment.");
        }
        catch (JsonException)
        {
            return Result<IReadOnlyCollection<PlaceProximityMediaCandidate>>.Fail(
                FailureKind.ExternalUnavailable, "Mapillary ha retornat una resposta no vàlida.");
        }
    }

    internal static string BuildRequestUri(decimal latitude, decimal longitude, int radiusMeters, int limit)
    {
        var latitudeRadians = (double)latitude * Math.PI / 180d;
        var latitudeDelta = radiusMeters / 111_320d;
        var longitudeScale = Math.Max(0.01d, Math.Cos(latitudeRadians));
        var longitudeDelta = radiusMeters / (111_320d * longitudeScale);
        var west = Math.Max(-180d, (double)longitude - longitudeDelta);
        var south = Math.Max(-90d, (double)latitude - latitudeDelta);
        var east = Math.Min(180d, (double)longitude + longitudeDelta);
        var north = Math.Min(90d, (double)latitude + latitudeDelta);
        var bbox = string.Join(',', new[] { west, south, east, north }.Select(Coordinate));
        return "images?fields=id,thumb_1024_url,computed_geometry,captured_at,computed_compass_angle,creator" +
            $"&bbox={bbox}&limit={Math.Clamp(limit, 1, 250)}";
    }

    internal static async Task<IReadOnlyCollection<PlaceProximityMediaCandidate>> ReadCandidatesAsync(
        Stream stream,
        decimal centerLatitude,
        decimal centerLongitude,
        int radiusMeters,
        int limit,
        CancellationToken cancellationToken = default)
    {
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!json.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) return [];

        var candidates = new List<PlaceProximityMediaCandidate>();
        foreach (var item in data.EnumerateArray())
        {
            if (!TryReadCandidate(item, centerLatitude, centerLongitude, out var candidate)) continue;
            if (candidate.DistanceMeters <= radiusMeters) candidates.Add(candidate);
        }

        return candidates
            .OrderBy(candidate => ReviewScore(candidate, centerLatitude, centerLongitude))
            .ThenBy(candidate => candidate.DistanceMeters)
            .ThenByDescending(candidate => candidate.CapturedAtUtc)
            .Take(Math.Clamp(limit, 1, 25))
            .ToArray();
    }

    private static bool TryReadCandidate(
        JsonElement item,
        decimal centerLatitude,
        decimal centerLongitude,
        out PlaceProximityMediaCandidate candidate)
    {
        candidate = null!;
        var id = item.TryGetProperty("id", out var idValue) ? idValue.GetString() : null;
        var preview = item.TryGetProperty("thumb_1024_url", out var previewValue) ? previewValue.GetString() : null;
        if (string.IsNullOrWhiteSpace(id) || !Uri.TryCreate(preview, UriKind.Absolute, out var previewUri) ||
            !TryReadCoordinates(item, out var latitude, out var longitude)) return false;

        var author = string.Empty;
        if (item.TryGetProperty("creator", out var creator) && creator.ValueKind == JsonValueKind.Object)
        {
            author = creator.TryGetProperty("username", out var username) ? username.GetString() ?? string.Empty : string.Empty;
        }
        if (string.IsNullOrWhiteSpace(author)) author = "Contribuïdor de Mapillary";

        DateTimeOffset? capturedAt = null;
        if (item.TryGetProperty("captured_at", out var captured) && captured.TryGetInt64(out var milliseconds))
            capturedAt = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);

        decimal? heading = null;
        if (item.TryGetProperty("computed_compass_angle", out var angle) && angle.TryGetDecimal(out var headingValue))
            heading = headingValue;

        var distance = DistanceMeters(centerLatitude, centerLongitude, latitude, longitude);
        var evidenceUri = new Uri($"https://www.mapillary.com/app/?pKey={Uri.EscapeDataString(id)}&focus=photo");
        candidate = new PlaceProximityMediaCandidate(
            id,
            previewUri,
            evidenceUri,
            author,
            capturedAt,
            heading,
            latitude,
            longitude,
            distance,
            $"© Mapillary · {author}");
        return true;
    }

    private static bool TryReadCoordinates(JsonElement item, out decimal latitude, out decimal longitude)
    {
        latitude = longitude = 0;
        if (!item.TryGetProperty("computed_geometry", out var geometry) ||
            !geometry.TryGetProperty("coordinates", out var coordinates) ||
            coordinates.ValueKind != JsonValueKind.Array || coordinates.GetArrayLength() < 2) return false;
        var enumerator = coordinates.EnumerateArray();
        enumerator.MoveNext();
        if (!enumerator.Current.TryGetDecimal(out longitude)) return false;
        enumerator.MoveNext();
        return enumerator.Current.TryGetDecimal(out latitude);
    }

    private static int DistanceMeters(decimal firstLatitude, decimal firstLongitude, decimal secondLatitude, decimal secondLongitude)
    {
        const double earthRadiusMeters = 6_371_000d;
        var firstLat = ToRadians((double)firstLatitude);
        var secondLat = ToRadians((double)secondLatitude);
        var latitudeDelta = secondLat - firstLat;
        var longitudeDelta = ToRadians((double)(secondLongitude - firstLongitude));
        var a = Math.Pow(Math.Sin(latitudeDelta / 2), 2) +
            Math.Cos(firstLat) * Math.Cos(secondLat) * Math.Pow(Math.Sin(longitudeDelta / 2), 2);
        return (int)Math.Round(earthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
    }

    private static decimal ReviewScore(
        PlaceProximityMediaCandidate candidate,
        decimal placeLatitude,
        decimal placeLongitude)
    {
        if (candidate.HeadingDegrees is null) return 180m + candidate.DistanceMeters;
        var targetBearing = BearingDegrees(candidate.Latitude, candidate.Longitude, placeLatitude, placeLongitude);
        var headingDifference = Math.Abs(((candidate.HeadingDegrees.Value - targetBearing + 540m) % 360m) - 180m);
        var distancePenalty = Math.Max(0m, Math.Abs(candidate.DistanceMeters - 25) - 10) * 0.7m;
        return headingDifference + distancePenalty;
    }

    private static decimal BearingDegrees(decimal firstLatitude, decimal firstLongitude, decimal secondLatitude, decimal secondLongitude)
    {
        var firstLat = ToRadians((double)firstLatitude);
        var secondLat = ToRadians((double)secondLatitude);
        var longitudeDelta = ToRadians((double)(secondLongitude - firstLongitude));
        var y = Math.Sin(longitudeDelta) * Math.Cos(secondLat);
        var x = Math.Cos(firstLat) * Math.Sin(secondLat) -
            Math.Sin(firstLat) * Math.Cos(secondLat) * Math.Cos(longitudeDelta);
        return (decimal)((Math.Atan2(y, x) * 180d / Math.PI + 360d) % 360d);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
    private static string Coordinate(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
