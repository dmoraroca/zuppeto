using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ValueObjects;

namespace Zuppeto.Application.Places;

/// <summary>
/// Place Details enrichment (cover, chips, visit notes). One reason to change: Google detail sync.
/// </summary>
internal sealed class PlaceGoogleDetailsEnricher(
    IPlaceRepository placeRepository,
    IExternalPlaceDetailsProvider detailsProvider,
    IPlaceCoverStorage coverStorage,
    IPlaceWebsitePageReader websitePageReader,
    PlaceCoverPhotoStore coverPhotoStore,
    IOptions<GooglePlacesIntegrationOptions> googlePlacesIntegrationOptions,
    ILogger<PlaceGoogleDetailsEnricher> logger)
{
    private int CoordinateCacheRetentionDays =>
        Math.Clamp(googlePlacesIntegrationOptions.Value.CoordinateCacheRetentionDays, 1, 366);

    internal async Task<Place> EnrichIfNeededAsync(
        Place place,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(place.GooglePlaceId)
            || place.DataProvenance is not (PlaceDataProvenance.GooglePlaces or PlaceDataProvenance.Mixed))
        {
            return place;
        }

        var cacheExpired = place.GoogleCoordinatesCachedUntil is null
            || nowUtc > place.GoogleCoordinatesCachedUntil.Value;
        var syncStale = place.LastGoogleSyncAt is null
            || nowUtc - place.LastGoogleSyncAt.Value > TimeSpan.FromDays(CoordinateCacheRetentionDays);
        var missingCover = string.IsNullOrWhiteSpace(place.CoverImageUrl);
        var missingHighlights = PlaceGoogleHighlights.NeedsChipRefresh(
                place.Features,
                place.Name,
                place.Description)
            || string.IsNullOrWhiteSpace(
                PlacePublicCopy.PublicNarrative(
                    place.Description,
                    place.Name,
                    place.Address.Line1,
                    place.Address.City));
        var recentCoverAttempt = coverStorage.HasRecentEnrichmentAttempt(
            place.Id,
            nowUtc,
            CoordinateCacheRetentionDays);
        if (recentCoverAttempt && !missingCover && !missingHighlights && !cacheExpired && !syncStale)
        {
            return place;
        }

        if (!(missingCover || missingHighlights || cacheExpired || syncStale))
        {
            return place;
        }

        var details = await detailsProvider.GetDetailsAsync(place.GooglePlaceId, cancellationToken);
        if (details is null)
        {
            coverStorage.MarkEnrichmentAttempt(place.Id, null);
            return place;
        }

        var cacheUntil = nowUtc.AddDays(CoordinateCacheRetentionDays);
        var coverUrl = await ResolveCoverUrlAsync(place, details, cancellationToken);
        var latitude = details.Latitude ?? place.Location.Latitude;
        var longitude = details.Longitude ?? place.Location.Longitude;
        var pricing = !string.IsNullOrWhiteSpace(details.PriceLabel)
            ? new Pricing(details.PriceLabel)
            : place.Pricing;
        var rating = details.Rating is > 0
            ? new RatingSnapshot(details.Rating.Value, details.ReviewCount ?? place.Rating.ReviewCount)
            : place.Rating;
        var petPolicy = BuildPetPolicy(details, place.PetPolicy);
        var visitNotes = PlaceVisitNotes.Combine(
            details.OpeningHours,
            details.Phone,
            details.Website,
            petPolicy.Notes);
        petPolicy = new PetPolicy(
            petPolicy.AcceptsDogs,
            petPolicy.AcceptsCats,
            petPolicy.Label,
            visitNotes);

        var typeLabel = PlaceTypeLabels.From(place.Type);
        var websiteText = await websitePageReader.TryReadVenueTextAsync(
            details.Website ?? string.Empty,
            string.IsNullOrWhiteSpace(details.Name) ? place.Name : details.Name.Trim(),
            cancellationToken);
        var features = PlaceGoogleHighlights.ToFeatureChips(details, place.Name, websiteText);
        if (!string.IsNullOrWhiteSpace(websiteText))
        {
            features = PlaceGoogleHighlights.MergeConfirmedChips(
                features,
                PlaceWebsiteAmenityCatalog.ConfirmedChips(websiteText));
        }

        if (features.Count == 0)
        {
            features = place.Features.ToArray();
        }

        var category = PlaceGoogleHighlights.CategoryLabel(features, typeLabel);
        var websiteLead = string.IsNullOrWhiteSpace(websiteText)
            ? null
            : PlaceWebsiteAmenityCatalog.Summary(websiteText);
        var description = PlacePublicCopy.ComposeQuickContext(
            category,
            details.EditorialSummary,
            websiteLead,
            string.IsNullOrWhiteSpace(details.Name) ? place.Name : details.Name.Trim(),
            details.Address ?? place.Address.Line1,
            place.Address.City);

        var neighborhood = PlaceAddressContext.NeighborhoodFromAddress(
            details.Address,
            place.Address.Neighborhood);
        var tags = PlaceAddressContext.ToContextTags(neighborhood, typeLabel);
        if (tags.Count == 0)
        {
            tags = place.Tags.ToArray();
        }

        var enriched = new Place(
            place.Id,
            string.IsNullOrWhiteSpace(details.Name) ? place.Name : details.Name.Trim(),
            place.Type,
            place.Name,
            string.IsNullOrWhiteSpace(description) ? place.Name : description,
            coverUrl ?? string.Empty,
            new PostalAddress(
                string.IsNullOrWhiteSpace(details.Address) ? place.Address.Line1 : details.Address.Trim(),
                place.Address.City,
                place.Address.Country,
                neighborhood ?? string.Empty),
            new GeoLocation(latitude, longitude),
            petPolicy,
            pricing,
            rating,
            place.DataProvenance,
            place.GooglePlaceId,
            cacheUntil,
            nowUtc,
            excludeFromOsmMap: false);

        enriched.ReplaceTags(tags);
        enriched.ReplaceFeatures(features);
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

        foreach (var photoReference in details.PhotoReferenceCandidates())
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

    private static PetPolicy BuildPetPolicy(PlaceExternalDetailsDto details, PetPolicy current)
    {
        if (details.AllowsDogs is null)
        {
            return PlacePublicCopy.IsPublicPetPolicyLabel(current.Label)
                ? current
                : new PetPolicy(
                    current.AcceptsDogs,
                    current.AcceptsCats,
                    PlacePublicCopy.UnspecifiedPetPolicyLabel,
                    current.Notes);
        }

        if (details.AllowsDogs == true)
        {
            return new PetPolicy(true, current.AcceptsCats, "Gossos permesos", current.Notes);
        }

        return new PetPolicy(false, current.AcceptsCats, "No es permeten gossos", current.Notes);
    }
}
