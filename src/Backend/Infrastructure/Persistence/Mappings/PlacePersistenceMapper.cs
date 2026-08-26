using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ValueObjects;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Mappings;

internal static class PlacePersistenceMapper
{
    private static readonly decimal WithheldLatitudeFallback = 40.25m;
    private static readonly decimal WithheldLongitudeFallback = -3.7m;

    public static Place ToDomain(PlaceRecord record)
    {
        var placeType = Enum.TryParse<PlaceType>(record.Type, ignoreCase: true, out var parsedType)
            ? parsedType
            : PlaceType.Service;

        // Plottable when lat/lng exist. Google provenance alone does not hide pins (map ≡ listing).
        // Redacted / withheld coordinates stay null in DB and are excluded from the map.
        var excludeFromOsmMap = record.Latitude is null || record.Longitude is null;
        var latitude = record.Latitude ?? WithheldLatitudeFallback;
        var longitude = record.Longitude ?? WithheldLongitudeFallback;
        var place = new Place(
            record.Id,
            record.Name,
            placeType,
            record.ShortDescription,
            record.Description,
            record.CoverImageUrl,
            new PostalAddress(
                record.AddressLine1,
                record.City,
                record.Country,
                record.Neighborhood ?? string.Empty),
            new GeoLocation(latitude, longitude),
            new PetPolicy(
                record.AcceptsDogs,
                record.AcceptsCats,
                record.PetPolicyLabel,
                record.PetPolicyNotes ?? string.Empty),
            new Pricing(record.PricingLabel),
            new RatingSnapshot(record.RatingAverage, record.ReviewCount),
            ParseDataProvenance(record.DataProvenance),
            record.GooglePlaceId,
            record.GoogleCoordinatesCachedUntil,
            record.LastGoogleSyncAt,
            excludeFromOsmMap);

        place.ReplaceTags(
            record.PlaceTags
                .Select(placeTag => placeTag.Tag.DisplayName));

        place.ReplaceFeatures(
            record.PlaceFeatures
                .Select(placeFeature => placeFeature.Feature.DisplayName));

        return place;
    }

    public static PlaceRecord ToRecord(Place place)
    {
        var record = new PlaceRecord();
        Apply(place, record);
        return record;
    }

    public static void Apply(Place place, PlaceRecord record)
    {
        record.Id = place.Id;
        record.Name = place.Name;
        record.Type = place.Type.ToString();
        record.ShortDescription = place.ShortDescription;
        record.Description = place.Description;
        record.CoverImageUrl = place.CoverImageUrl;
        record.AddressLine1 = place.Address.Line1;
        record.City = place.Address.City;
        record.Country = place.Address.Country;
        record.Neighborhood = place.Address.Neighborhood;
        // Persist coordinates even when the stored ExcludeFromOsmMap flag is true: Google cache
        // (≤30 days) needs lat/lng in DB. Map rendering uses null lat/lng (redaction) as the gate.
        record.Latitude = place.Location.Latitude;
        record.Longitude = place.Location.Longitude;
        record.ExcludeFromOsmMap = place.ExcludeFromOsmMap;
        record.AcceptsDogs = place.PetPolicy.AcceptsDogs;
        record.AcceptsCats = place.PetPolicy.AcceptsCats;
        record.PetPolicyLabel = place.PetPolicy.Label;
        record.PetPolicyNotes = place.PetPolicy.Notes;
        record.PricingLabel = place.Pricing.DisplayLabel;
        record.RatingAverage = place.Rating.Average;
        record.ReviewCount = place.Rating.ReviewCount;
        record.DataProvenance = place.DataProvenance.ToString();
        record.GooglePlaceId = place.GooglePlaceId;
        record.GoogleCoordinatesCachedUntil = place.GoogleCoordinatesCachedUntil;
        record.LastGoogleSyncAt = place.LastGoogleSyncAt;
    }

    public static void SyncCollections(Place place, PlaceRecord record)
    {
        var desiredTags = Normalize(place.Tags);
        var desiredFeatures = Normalize(place.Features);

        var tagsToRemove = record.PlaceTags
            .Where(placeTag => !desiredTags.Contains(placeTag.Tag.Code))
            .ToArray();

        foreach (var placeTag in tagsToRemove)
        {
            record.PlaceTags.Remove(placeTag);
        }

        foreach (var tagValue in desiredTags)
        {
            if (record.PlaceTags.Any(placeTag => placeTag.Tag.Code == tagValue))
            {
                continue;
            }

            record.PlaceTags.Add(new PlaceTagRecord
            {
                PlaceId = record.Id,
                Tag = new TagRecord
                {
                    Code = tagValue,
                    DisplayName = tagValue
                }
            });
        }

        var featuresToRemove = record.PlaceFeatures
            .Where(placeFeature => !desiredFeatures.Contains(placeFeature.Feature.Code))
            .ToArray();

        foreach (var placeFeature in featuresToRemove)
        {
            record.PlaceFeatures.Remove(placeFeature);
        }

        foreach (var featureValue in desiredFeatures)
        {
            if (record.PlaceFeatures.Any(placeFeature => placeFeature.Feature.Code == featureValue))
            {
                continue;
            }

            record.PlaceFeatures.Add(new PlaceFeatureRecord
            {
                PlaceId = record.Id,
                Feature = new FeatureRecord
                {
                    Code = featureValue,
                    DisplayName = featureValue
                }
            });
        }
    }

    private static HashSet<string> Normalize(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static PlaceDataProvenance ParseDataProvenance(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return PlaceDataProvenance.Internal;
        }

        return Enum.TryParse<PlaceDataProvenance>(value, ignoreCase: true, out var parsed)
            ? parsed
            : PlaceDataProvenance.Internal;
    }
}
