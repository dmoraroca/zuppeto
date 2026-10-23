using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

internal sealed class PlaceResponseMapper(IPlaceCoverStorage coverStorage, PlaceMediaSelectionPolicy? configuredMediaSelectionPolicy = null)
{
    private readonly PlaceMediaSelectionPolicy mediaSelectionPolicy = configuredMediaSelectionPolicy ?? new PlaceMediaSelectionPolicy();
    internal PlaceSummaryDto ToSummary(Place place, IReadOnlyCollection<PlaceMediaAsset>? media = null)
    {
        var publicMedia = (media ?? [])
            .Where(asset => asset.VerificationStatus == PlaceMediaVerificationStatus.Verified)
            .Where(asset => asset.IdentityVerified && asset.CanBePrimary)
            .ToArray();
        var (cacheExpired, requiresGoogleMap) = ComputeGoogleCoordinateFlags(place);
        var excludeFromOsmMap = place.ExcludeFromOsmMap && !requiresGoogleMap;
        var visit = PlaceVisitNotes.Split(place.PetPolicy.Notes);
        var attribution = coverStorage.ReadAttribution(place.Id);
        var primaryMedia = mediaSelectionPolicy.SelectPrimary(publicMedia);
        return new PlaceSummaryDto(
            place.Id,
            place.Name,
            place.Type.ToString(),
            PlacePublicCopy.SanitizeDescription(place.ShortDescription, place.Name),
            PlacePublicCopy.PublicNarrative(place.Description, place.Name, place.Address.Line1, place.Address.City),
            primaryMedia is null ? LegacyCoverUrl(place) : ThumbnailUrl(primaryMedia.ServedUri.ToString()),
            place.Address.Line1,
            place.Address.City,
            place.Address.Country,
            place.Address.Neighborhood,
            place.Location?.Latitude,
            place.Location?.Longitude,
            place.DataProvenance.ToString(),
            place.GooglePlaceId,
            place.GoogleCoordinatesCachedUntil,
            place.LastGoogleSyncAt,
            cacheExpired,
            requiresGoogleMap,
            excludeFromOsmMap,
            place.PetPolicy.AcceptsDogs,
            place.PetPolicy.AcceptsCats,
            PlacePublicCopy.SanitizePetPolicyLabel(place.PetPolicy.Label),
            visit.PetNotes,
            PlacePublicCopy.PublicPricingLabel(place.Pricing.DisplayLabel),
            place.Rating.Average,
            place.Rating.ReviewCount,
            place.Tags.ToArray(),
            place.Features.ToArray(),
            visit.Hours,
            visit.Phone,
            visit.Website,
            PlaceGoogleHighlights.CategoryLabel(place.Features, CommercialCategoryLabels.From(place.CommercialCategory, place.Type)),
            primaryMedia?.Attribution ?? attribution?.AuthorName,
            primaryMedia?.EvidenceUri?.ToString() ?? attribution?.SourceUri,
            place.TerritorialCountryId,
            place.TerritorialUnitId,
            place.PetPolicy.Status.ToString(),
            place.PetPolicy.Species.Select(x => x.Code).ToArray(),
            place.PetPolicy.Conditions.Select(x => new PetConditionDto(x.Code, x.Value)).ToArray(),
            Media: publicMedia.Take(5).Select(PlaceMediaApplicationService.ToDto).ToArray(),
            CommercialCategory: place.CommercialCategory);
    }

    private static string ThumbnailUrl(string servedUrl) =>
        servedUrl.EndsWith("/optimized", StringComparison.OrdinalIgnoreCase)
            ? $"{servedUrl[..^"optimized".Length]}thumbnail"
            : servedUrl;

    internal PlaceSummaryDto ToSummary(PlacePublicSearchRow row) => ToSummary(row.Place) with
    {
        DistanceKm = row.DistanceKm,
        IsPetPolicyVerified = row.IsPetPolicyVerified,
        PetPolicyVerifiedAtUtc = row.PetPolicyVerifiedAtUtc,
        UpdatedAtUtc = row.UpdatedAtUtc,
        BusinessStatus = row.BusinessStatus
    };

    internal PlaceDetailDto ToDetail(Place place, IReadOnlyCollection<PlaceMediaAsset>? media = null)
    {
        var summary = ToSummary(place, media);
        return new PlaceDetailDto(
            summary.Id,
            summary.Name,
            summary.Type,
            summary.ShortDescription,
            summary.Description,
            summary.CoverImageUrl,
            summary.AddressLine1,
            summary.City,
            summary.Country,
            summary.Neighborhood,
            summary.Latitude,
            summary.Longitude,
            summary.DataProvenance,
            summary.GooglePlaceId,
            summary.GoogleCoordinatesCachedUntil,
            summary.LastGoogleSyncAt,
            summary.GoogleCoordinatesCacheExpired,
            summary.RequiresGoogleMapForGoogleCoordinates,
            summary.ExcludeFromOsmMap,
            summary.AcceptsDogs,
            summary.AcceptsCats,
            summary.PetPolicyLabel,
            summary.PetPolicyNotes,
            summary.PricingLabel,
            summary.RatingAverage,
            summary.ReviewCount,
            summary.Tags,
            summary.Features,
            summary.CoverAttribution,
            summary.CoverSourceUri,
            summary.OpeningHours,
            summary.Phone,
            summary.Website,
            summary.CategoryLabel,
            summary.CountryId,
            summary.TerritorialUnitId,
            summary.PetAccessStatus,
            summary.PetSpecies,
            summary.PetConditions,
            summary.IsPetPolicyVerified,
            summary.PetPolicyVerifiedAtUtc,
            summary.UpdatedAtUtc,
            summary.BusinessStatus,
            summary.Media,
            summary.CommercialCategory);
    }

    private static (bool CacheExpired, bool RequiresGoogleMap) ComputeGoogleCoordinateFlags(Place place)
    {
        var now = DateTimeOffset.UtcNow;
        if (place.DataProvenance is not (PlaceDataProvenance.GooglePlaces or PlaceDataProvenance.Mixed))
        {
            return (CacheExpired: false, RequiresGoogleMap: false);
        }

        if (place.GoogleCoordinatesCachedUntil is null)
        {
            return (CacheExpired: true, RequiresGoogleMap: false);
        }

        var expired = now > place.GoogleCoordinatesCachedUntil.Value;
        return (CacheExpired: expired, RequiresGoogleMap: !expired);
    }

    private static string LegacyCoverUrl(Place place) =>
        place.DataProvenance is PlaceDataProvenance.GooglePlaces or PlaceDataProvenance.Mixed
            ? string.Empty
            : place.CoverImageUrl;
}
