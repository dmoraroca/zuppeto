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

        if (request.Latitude is < -90 or > 90)
        {
            result.Add(nameof(request.Latitude), "La latitud ha d'estar entre -90 i 90.");
        }

        if (request.Longitude is < -180 or > 180)
        {
            result.Add(nameof(request.Longitude), "La longitud ha d'estar entre -180 i 180.");
        }

        if (!request.AcceptsDogs && !request.AcceptsCats)
        {
            result.Add(nameof(request.AcceptsDogs), "Cal acceptar com a mínim gossos o gats.");
        }

        if (request.RatingAverage is < 0 or > 5)
        {
            result.Add(nameof(request.RatingAverage), "El rating ha d'estar entre 0 i 5.");
        }

        if (request.ReviewCount < 0)
        {
            result.Add(nameof(request.ReviewCount), "El nombre de ressenyes no pot ser negatiu.");
        }

        PlaceDataProvenance? parsedProvenance = null;
        if (!string.IsNullOrWhiteSpace(request.DataProvenance))
        {
            if (!Enum.TryParse<PlaceDataProvenance>(request.DataProvenance.Trim(), ignoreCase: true, out var pv))
            {
                result.Add(nameof(request.DataProvenance), "La procedència de dades no és vàlida.");
            }
            else
            {
                parsedProvenance = pv;
            }
        }

        var hasGooglePlaceId = !string.IsNullOrWhiteSpace(request.GooglePlaceId);

        if (hasGooglePlaceId && parsedProvenance is not (PlaceDataProvenance.GooglePlaces or PlaceDataProvenance.Mixed))
        {
            result.Add(
                nameof(request.DataProvenance),
                "Si s’indica Google Place ID, la procedència ha de ser GooglePlaces o Mixed.");
        }

        if (parsedProvenance is PlaceDataProvenance.GooglePlaces or PlaceDataProvenance.Mixed && !hasGooglePlaceId)
        {
            result.Add(nameof(request.GooglePlaceId), "El Google Place ID és obligatori quan la procedència és Google Places o mixta.");
        }

        return result;
    }
}
