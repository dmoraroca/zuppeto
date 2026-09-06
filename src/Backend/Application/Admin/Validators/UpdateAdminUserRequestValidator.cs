using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Admin.Validators;

public sealed class UpdateAdminUserRequestValidator : IValidator<UpdateAdminUserRequest>
{
    public ValidationResult Validate(UpdateAdminUserRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            result.Add(nameof(request.DisplayName), "Display name is required.");
        }
        else if (request.DisplayName.Trim().Length < 3)
        {
            result.Add(nameof(request.DisplayName), "Display name must be at least 3 characters long.");
        }

        if (string.IsNullOrWhiteSpace(request.City))
        {
            result.Add(nameof(request.City), "City is required.");
        }
        else if (request.City.Trim().Length < 2)
        {
            result.Add(nameof(request.City), "City must be at least 2 characters long.");
        }

        if (string.IsNullOrWhiteSpace(request.Country))
        {
            result.Add(nameof(request.Country), "Country is required.");
        }
        else if (request.Country.Trim().Length < 2)
        {
            result.Add(nameof(request.Country), "Country must be at least 2 characters long.");
        }

        if (string.IsNullOrWhiteSpace(request.Comments))
        {
            result.Add(nameof(request.Comments), "Comments are required.");
        }

        return result;
    }
}
