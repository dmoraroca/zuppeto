namespace Zuppeto.Application.Places;

/// <summary>
/// Detall extern d'un lloc conegut. No assumeix cap proveïdor concret.
/// </summary>
public interface IExternalPlaceDetailsProvider
{
    Task<PlaceExternalDetailsDto?> GetDetailsAsync(
        string externalPlaceId,
        CancellationToken cancellationToken = default);
}

public interface IExternalPlacePhotoProvider
{
    Task<byte[]?> DownloadPhotoAsync(
        string photoReferenceOrName,
        CancellationToken cancellationToken = default);
}

public sealed record PlaceExternalDetailsDto(
    string GooglePlaceId,
    string? Name,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    decimal? Rating,
    int? ReviewCount,
    string? PriceLabel,
    string? EditorialSummary,
    bool? AllowsDogs,
    bool? OutdoorSeating,
    bool? Reservable,
    bool? Takeout,
    bool? Restroom,
    bool? GoodForChildren,
    string? PhotoReference,
    string? PhotoAttribution,
    string? PhotoSourceUri,
    string? Phone = null,
    string? Website = null,
    string? OpeningHours = null,
    IReadOnlyList<string>? Types = null,
    IReadOnlyList<string>? ExtraPhotoReferences = null,
    string? PrimaryType = null,
    string? PrimaryTypeDisplayName = null)
{
    public IEnumerable<string> PhotoReferenceCandidates()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in new[] { PhotoReference }.Concat(ExtraPhotoReferences ?? []))
        {
            var value = candidate?.Trim() ?? string.Empty;
            if (value.Length == 0 || !seen.Add(value))
            {
                continue;
            }

            yield return value;
        }
    }
}
