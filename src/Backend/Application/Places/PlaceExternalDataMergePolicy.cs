using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ValueObjects;

namespace Zuppeto.Application.Places;

/// <summary>
/// Política pura de precedència: manual &gt; extern. No fa I/O ni coneix el proveïdor.
/// </summary>
internal sealed class PlaceExternalDataMergePolicy
{
    internal Place Merge(
        Place place,
        PlaceExternalDetailsDto details,
        string coverUrl,
        string websiteText,
        DateTimeOffset synchronizedAtUtc,
        int retentionDays)
    {
        var coordinatesAreManual = place.IsManuallyMaintained(PlaceManualFields.Coordinates);
        DateTimeOffset? coordinatesCachedUntil = coordinatesAreManual
            ? null
            : synchronizedAtUtc.AddDays(retentionDays);
        var latitude = coordinatesAreManual
            ? place.Location.Latitude
            : details.Latitude ?? place.Location.Latitude;
        var longitude = coordinatesAreManual
            ? place.Location.Longitude
            : details.Longitude ?? place.Location.Longitude;
        var pricing = !place.IsManuallyMaintained(PlaceManualFields.Pricing)
            && !string.IsNullOrWhiteSpace(details.PriceLabel)
                ? new Pricing(details.PriceLabel)
                : place.Pricing;
        var rating = !place.IsManuallyMaintained(PlaceManualFields.Rating) && details.Rating is > 0
            ? new RatingSnapshot(details.Rating.Value, details.ReviewCount ?? place.Rating.ReviewCount)
            : place.Rating;
        var petPolicy = MergePetPolicy(place, details);
        var features = MergeFeatures(place, details, websiteText);
        var typeLabel = PlaceTypeLabels.From(place.Type);
        var neighborhood = place.IsManuallyMaintained(PlaceManualFields.Address)
            ? place.Address.Neighborhood
            : PlaceAddressContext.NeighborhoodFromAddress(details.Address, place.Address.Neighborhood)
                ?? string.Empty;
        var tags = MergeTags(place, neighborhood, typeLabel);
        var name = place.IsManuallyMaintained(PlaceManualFields.Identity)
            || string.IsNullOrWhiteSpace(details.Name)
                ? place.Name
                : details.Name.Trim();
        var description = MergeDescription(place, details, websiteText, features, typeLabel, name);

        var merged = new Place(
            place.Id,
            name,
            place.Type,
            place.ShortDescription,
            description,
            coverUrl,
            new PostalAddress(
                place.IsManuallyMaintained(PlaceManualFields.Address)
                    || string.IsNullOrWhiteSpace(details.Address)
                        ? place.Address.Line1
                        : details.Address.Trim(),
                place.Address.City,
                place.Address.Country,
                neighborhood),
            new GeoLocation(latitude, longitude),
            petPolicy,
            pricing,
            rating,
            place.ManualFields == PlaceManualFields.None
                ? PlaceDataProvenance.GooglePlaces
                : PlaceDataProvenance.Mixed,
            place.GooglePlaceId,
            coordinatesCachedUntil,
            synchronizedAtUtc,
            excludeFromOsmMap: place.ExcludeFromOsmMap,
            place.ManualFields);

        merged.ReplaceTags(tags);
        merged.ReplaceFeatures(features);
        return merged;
    }

    private static PetPolicy MergePetPolicy(Place place, PlaceExternalDetailsDto details)
    {
        if (place.IsManuallyMaintained(PlaceManualFields.PetPolicy))
        {
            return place.PetPolicy;
        }

        var current = place.PetPolicy;
        var basePolicy = details.AllowsDogs switch
        {
            true => new PetPolicy(true, current.AcceptsCats, "Gossos permesos", current.Notes),
            false => new PetPolicy(false, current.AcceptsCats, "No es permeten gossos", current.Notes),
            _ when PlacePublicCopy.IsPublicPetPolicyLabel(current.Label) => current,
            _ => new PetPolicy(
                current.AcceptsDogs,
                current.AcceptsCats,
                PlacePublicCopy.UnspecifiedPetPolicyLabel,
                current.Notes)
        };
        var notes = PlaceVisitNotes.Combine(
            details.OpeningHours,
            details.Phone,
            details.Website,
            basePolicy.Notes);
        return new PetPolicy(
            basePolicy.AcceptsDogs,
            basePolicy.AcceptsCats,
            basePolicy.Label,
            notes);
    }

    private static IReadOnlyCollection<string> MergeFeatures(
        Place place,
        PlaceExternalDetailsDto details,
        string websiteText)
    {
        if (place.IsManuallyMaintained(PlaceManualFields.Features))
        {
            return place.Features;
        }

        var features = PlaceGoogleHighlights.ToFeatureChips(details, place.Name, websiteText);
        if (!string.IsNullOrWhiteSpace(websiteText))
        {
            features = PlaceGoogleHighlights.MergeConfirmedChips(
                features,
                PlaceWebsiteAmenityCatalog.ConfirmedChips(websiteText));
        }

        return features.Count == 0 ? place.Features : features;
    }

    private static IReadOnlyCollection<string> MergeTags(
        Place place,
        string neighborhood,
        string typeLabel)
    {
        if (place.IsManuallyMaintained(PlaceManualFields.Tags))
        {
            return place.Tags;
        }

        var tags = PlaceAddressContext.ToContextTags(neighborhood, typeLabel);
        return tags.Count == 0 ? place.Tags : tags;
    }

    private static string MergeDescription(
        Place place,
        PlaceExternalDetailsDto details,
        string websiteText,
        IReadOnlyCollection<string> features,
        string typeLabel,
        string name)
    {
        if (place.IsManuallyMaintained(PlaceManualFields.Descriptions))
        {
            return place.Description;
        }

        var category = PlaceGoogleHighlights.CategoryLabel(features, typeLabel);
        var websiteLead = string.IsNullOrWhiteSpace(websiteText)
            ? null
            : PlaceWebsiteAmenityCatalog.Summary(websiteText);
        var description = PlacePublicCopy.ComposeQuickContext(
            category,
            details.EditorialSummary,
            websiteLead,
            name,
            details.Address ?? place.Address.Line1,
            place.Address.City);
        return string.IsNullOrWhiteSpace(description) ? place.Description : description;
    }
}
