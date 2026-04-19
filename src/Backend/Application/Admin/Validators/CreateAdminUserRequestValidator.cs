using YepPet.Application.Validation;

namespace YepPet.Application.Admin.Validators;

public sealed class CreateAdminUserRequestValidator : IValidator<CreateAdminUserRequest>
{
    public ValidationResult Validate(CreateAdminUserRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            result.Add(nameof(request.Email), "Email is required.");
        }
        else if (!request.Email.Contains('@', StringComparison.Ordinal))
        {
            result.Add(nameof(request.Email), "Email format is invalid.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            result.Add(nameof(request.Password), "Password is required.");
        }
        else if (request.Password.Trim().Length < 6)
        {
            result.Add(nameof(request.Password), "Password must be at least 6 characters.");
        }

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

        if (!string.IsNullOrWhiteSpace(request.Role) && request.Role.Trim().Length > 32)
        {
            result.Add(nameof(request.Role), "Role is too long.");
        }

        return result;
    }
}
