using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Users.Validators;

public sealed class UserRegistrationRequestValidator : IValidator<UserRegistrationRequest>
{
    public ValidationResult Validate(UserRegistrationRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            result.Add(nameof(request.Email), "L’email és obligatori.");
        }
        else if (!request.Email.Contains('@', StringComparison.Ordinal))
        {
            result.Add(nameof(request.Email), "El format de l’email no és vàlid.");
        }

        if (string.IsNullOrWhiteSpace(request.PasswordHash))
        {
            result.Add(nameof(request.PasswordHash), "La contrasenya és obligatòria.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            result.Add(nameof(request.DisplayName), "El nom visible és obligatori.");
        }

        if (!string.Equals(request.Role?.Trim(), "User", StringComparison.OrdinalIgnoreCase))
        {
            result.Add(nameof(request.Role), "El rol no és vàlid.");
        }

        return result;
    }
}
