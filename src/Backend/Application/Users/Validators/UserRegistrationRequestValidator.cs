using YepPet.Application.Validation;

namespace YepPet.Application.Users.Validators;

public sealed class UserRegistrationRequestValidator : IValidator<UserRegistrationRequest>
{
    public ValidationResult Validate(UserRegistrationRequest request)
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

        if (string.IsNullOrWhiteSpace(request.PasswordHash))
        {
            result.Add(nameof(request.PasswordHash), "Password is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            result.Add(nameof(request.DisplayName), "Display name is required.");
        }

        if (!string.Equals(request.Role?.Trim(), "User", StringComparison.OrdinalIgnoreCase))
        {
            result.Add(nameof(request.Role), "Role is invalid.");
        }

        return result;
    }
}
