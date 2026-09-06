using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Admin.Validators;

public sealed class CreateCityRequestValidator : IValidator<CreateCityRequest>
{
    public ValidationResult Validate(CreateCityRequest request)
    {
        var result = ValidationResult.Success();

        if (request.CountryId == Guid.Empty)
        {
            result.Add(nameof(request.CountryId), "El país és obligatori.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            result.Add(nameof(request.Name), "El nom és obligatori.");
        }

        if (request.Latitude is not null && (request.Latitude < -90m || request.Latitude > 90m))
        {
            result.Add(nameof(request.Latitude), "La latitud ha de ser entre -90 i 90.");
        }

        if (request.Longitude is not null && (request.Longitude < -180m || request.Longitude > 180m))
        {
            result.Add(nameof(request.Longitude), "La longitud ha de ser entre -180 i 180.");
        }

        return result;
    }
}
