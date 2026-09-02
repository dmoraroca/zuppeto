using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

internal sealed class PlaceResponseMapper(IPlaceCoverStorage coverStorage)
{
    internal PlaceSummaryDto ToSummary(Place place)
    {
        var (cacheExpired, requiresGoogleMap) = ComputeGoogleCoordinateFlags(place);
        var excludeFromOsmMap = place.ExcludeFromOsmMap && !requiresGoogleMap;
        var visit = PlaceVisitNotes.Split(place.PetPolicy.Notes);
        return new PlaceSummaryDto(
            place.Id,
            place.Name,
            place.Type.ToString(),
            PlacePublicCopy.SanitizeDescription(place.ShortDescription, place.Name),
            PlacePublicCopy.PublicNarrative(place.Description, place.Name, place.Address.Line1, place.Address.City),
            place.CoverImageUrl,
            place.Address.Line1,
            place.Address.City,
            place.Address.Country,
            place.Address.Neighborhood,
            place.Location.Latitude,
            place.Location.Longitude,
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
            PlaceGoogleHighlights.CategoryLabel(place.Features, PlaceTypeLabels.From(place.Type)));
    }

    internal PlaceDetailDto ToDetail(Place place)
    {
        var summary = ToSummary(place);
        var attribution = coverStorage.ReadAttribution(place.Id);
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
            attribution?.AuthorName,
            attribution?.SourceUri,
            summary.OpeningHours,
            summary.Phone,
            summary.Website,
            summary.CategoryLabel);
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
}
