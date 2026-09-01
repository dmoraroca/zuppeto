namespace Zuppeto.Application.Places;

/// <summary>
/// Loads public website copy for a venue. The HTTP adapter may follow one same-host link
/// that names the place; it does not invent amenities.
/// </summary>
public interface IPlaceWebsitePageReader
{
    Task<string?> TryReadVenueTextAsync(
        string websiteUrl,
        string placeName,
        CancellationToken cancellationToken = default);
}
