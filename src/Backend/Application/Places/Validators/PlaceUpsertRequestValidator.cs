using Zuppeto.Application.Validation;
using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places.Validators;

public sealed class PlaceUpsertRequestValidator : IValidator<PlaceUpsertRequest>
{
    public ValidationResult Validate(PlaceUpsertRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            result.Add(nameof(request.Name), "El nom és obligatori.");
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            result.Add(nameof(request.Type), "El tipus és obligatori.");
        }
        else if (!Enum.TryParse<Domain.Places.PlaceType>(request.Type, true, out _))
        {
            result.Add(nameof(request.Type), "El tipus de lloc no és vàlid.");
        }

        if (string.IsNullOrWhiteSpace(request.AddressLine1))
        {
            result.Add(nameof(request.AddressLine1), "L'adreça és obligatòria.");
        }

        if (string.IsNullOrWhiteSpace(request.City))
        {
            result.Add(nameof(request.City), "La ciutat és obligatòria.");
        }

        if (string.IsNullOrWhiteSpace(request.Country))
        {
            result.Add(nameof(request.Country), "El país és obligatori.");
        }

        if ((request.Latitude is null) != (request.Longitude is null))
        {
            result.Add(nameof(request.Latitude), "La latitud i la longitud s’han d’informar conjuntament.");
        }
        else if (request.Latitude is < -90 or > 90)
        {
            result.Add(nameof(request.Latitude), "La latitud ha d'estar entre -90 i 90.");
        }

        if (request.Longitude is < -180 or > 180)
        {
            result.Add(nameof(request.Longitude), "La longitud ha d'estar entre -180 i 180.");
        }

        if (!Enum.TryParse<PetAccessStatus>(request.PetAccessStatus, true, out _))
        {
            result.Add(nameof(request.PetAccessStatus), "L’estat d’admissió no és vàlid.");
        }

        if (request.RatingAverage is < 0 or > 5)
        {
            result.Add(nameof(request.RatingAverage), "El rating ha d'estar entre 0 i 5.");
        }

        if (request.ReviewCount < 0)
        {
            result.Add(nameof(request.ReviewCount), "El nombre de ressenyes no pot ser negatiu.");
        }

        if ((request.CountryId is null) != (request.TerritorialUnitId is null))
        {
            result.Add(nameof(request.TerritorialUnitId), "El país i la localitat territorial s’han d’informar conjuntament.");
        }

        var requestedProvenance = request.DataProvenance?.Trim();
        if (!string.IsNullOrWhiteSpace(request.GooglePlaceId)
            || request.GoogleCoordinatesCachedUntil is not null
            || request.LastGoogleSyncAt is not null
            || (!string.IsNullOrWhiteSpace(requestedProvenance)
                && !string.Equals(requestedProvenance, nameof(PlaceDataProvenance.Internal), StringComparison.OrdinalIgnoreCase)))
        {
            result.Add(nameof(request.GooglePlaceId), "Les metadades històriques de Google Places són només de lectura.");
        }

        return result;
    }
}
