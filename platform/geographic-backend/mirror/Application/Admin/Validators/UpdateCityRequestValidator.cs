using YepPet.Application.Validation;

namespace YepPet.Application.Admin.Validators;

public sealed class UpdateCityRequestValidator : IValidator<UpdateCityRequest>
{
    public ValidationResult Validate(UpdateCityRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            result.Add(nameof(request.Name), "Name is required.");
        }
        else if (request.Name.Trim().Length > 200)
        {
            result.Add(nameof(request.Name), "Name is too long.");
        }

        if (request.Latitude is not null && (request.Latitude < -90m || request.Latitude > 90m))
        {
            result.Add(nameof(request.Latitude), "Latitude must be between -90 and 90.");
        }

        if (request.Longitude is not null && (request.Longitude < -180m || request.Longitude > 180m))
        {
            result.Add(nameof(request.Longitude), "Longitude must be between -180 and 180.");
        }

        return result;
    }
}
