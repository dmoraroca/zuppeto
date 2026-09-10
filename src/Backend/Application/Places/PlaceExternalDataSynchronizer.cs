using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

/// <summary>
/// Sincronitza una referència externa sense modificar cap camp governat manualment.
/// </summary>
internal sealed class PlaceExternalDataSynchronizer(
    IPlaceRepository placeRepository,
    IExternalPlaceDetailsProvider detailsProvider,
    IPlaceCoverStorage coverStorage,
    IPlaceWebsitePageReader websitePageReader,
    PlaceCoverPhotoStore coverPhotoStore,
    PlaceExternalDataMergePolicy mergePolicy,
    IOptions<PlaceExternalIntegrationOptions> externalIntegrationOptions,
    IExternalPlaceCallPolicy externalCallPolicy,
    ILogger<PlaceExternalDataSynchronizer> logger)
    : IPlaceExternalDataSynchronizer
{
    private int CoordinateCacheRetentionDays =>
        Math.Clamp(externalIntegrationOptions.Value.CoordinateCacheRetentionDays, 1, 366);

    public async Task<Place?> SynchronizeAsync(
        Guid placeId,
        CancellationToken cancellationToken = default)
    {
        var place = await placeRepository.GetByIdAsync(placeId, cancellationToken);
        return place is null
            ? null
            : await EnrichIfNeededAsync(place, DateTimeOffset.UtcNow, cancellationToken);
    }

    internal async Task<Place> EnrichIfNeededAsync(
        Place place,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (!externalCallPolicy.AllowsBillableCalls
            || string.IsNullOrWhiteSpace(place.GooglePlaceId)
            || place.DataProvenance is not (PlaceDataProvenance.GooglePlaces or PlaceDataProvenance.Mixed))
        {
            return place;
        }

        var coordinatesAreManual = place.IsManuallyMaintained(PlaceManualFields.Coordinates);
        var cacheExpired = !coordinatesAreManual
            && (place.GoogleCoordinatesCachedUntil is null
                || nowUtc > place.GoogleCoordinatesCachedUntil.Value);
        var syncStale = place.LastGoogleSyncAt is null
            || nowUtc - place.LastGoogleSyncAt.Value > TimeSpan.FromDays(CoordinateCacheRetentionDays);
        var missingCover = !place.IsManuallyMaintained(PlaceManualFields.Cover)
            && string.IsNullOrWhiteSpace(place.CoverImageUrl);
        var missingHighlights = !place.IsManuallyMaintained(PlaceManualFields.Features)
            && (PlaceGoogleHighlights.NeedsChipRefresh(
                place.Features,
                place.Name,
                place.Description)
            || string.IsNullOrWhiteSpace(
                PlacePublicCopy.PublicNarrative(
                    place.Description,
                    place.Name,
                    place.Address.Line1,
                    place.Address.City)));
        var recentCoverAttempt = coverStorage.HasRecentEnrichmentAttempt(
            place.Id,
            nowUtc,
            CoordinateCacheRetentionDays);
        var hasExternallyManagedFields = place.ManualFields != PlaceManualFields.All;
        if ((!missingCover || recentCoverAttempt)
            && !missingHighlights
            && !cacheExpired
            && (!syncStale || !hasExternallyManagedFields))
        {
            return place;
        }

        var details = await detailsProvider.GetDetailsAsync(place.GooglePlaceId, cancellationToken);
        if (details is null)
        {
            coverStorage.MarkEnrichmentAttempt(place.Id, null);
            return place;
        }

        var coverUrl = place.IsManuallyMaintained(PlaceManualFields.Cover)
            ? place.CoverImageUrl
            : await ResolveCoverUrlAsync(place, details, cancellationToken);
        var needsWebsiteContext = !place.IsManuallyMaintained(PlaceManualFields.Descriptions)
            || !place.IsManuallyMaintained(PlaceManualFields.Features);
        var websiteText = needsWebsiteContext
            ? await websitePageReader.TryReadVenueTextAsync(
                details.Website ?? string.Empty,
                string.IsNullOrWhiteSpace(details.Name) ? place.Name : details.Name.Trim(),
                cancellationToken) ?? string.Empty
            : string.Empty;
        var enriched = mergePolicy.Merge(
            place,
            details,
            coverUrl ?? string.Empty,
            websiteText,
            nowUtc,
            CoordinateCacheRetentionDays);
        try
        {
            await placeRepository.UpdateAsync(enriched, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Could not persist Google enrichment for place {PlaceId}; returning in-memory chips.",
                place.Id);
        }

        return enriched;
    }

    private async Task<string?> ResolveCoverUrlAsync(
        Place place,
        PlaceExternalDetailsDto details,
        CancellationToken cancellationToken)
    {
        var coverUrl = place.CoverImageUrl;
        if (!string.IsNullOrWhiteSpace(coverUrl))
        {
            return coverUrl;
        }

        var maximumDownloads = Math.Clamp(
            externalIntegrationOptions.Value.MaxPhotoDownloadsPerPlace,
            0,
            5);
        foreach (var photoReference in details.PhotoReferenceCandidates().Take(maximumDownloads))
        {
            coverUrl = await coverPhotoStore.TryStoreFromReferenceAsync(
                place.Id,
                photoReference,
                new PlaceCoverAttribution(details.PhotoAttribution, details.PhotoSourceUri),
                cancellationToken);
            if (!string.IsNullOrWhiteSpace(coverUrl))
            {
                return coverUrl;
            }
        }

        coverStorage.MarkEnrichmentAttempt(
            place.Id,
            new PlaceCoverAttribution(details.PhotoAttribution, details.PhotoSourceUri));
        return coverUrl;
    }
}
