using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Auth.Validators;

public sealed class LoginRequestValidator : IValidator<LoginRequest>
{
    public ValidationResult Validate(LoginRequest request)
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

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            result.Add(nameof(request.Password), "La contrasenya és obligatòria.");
        }
        else if (request.Password.Trim().Length < 6)
        {
            result.Add(nameof(request.Password), "La contrasenya ha de tenir com a mínim 6 caràcters.");
        }

        return result;
    }
}
