using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Admin.Validators;

public sealed class CreateAdminUserRequestValidator : IValidator<CreateAdminUserRequest>
{
    public ValidationResult Validate(CreateAdminUserRequest request)
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

        if (!string.Equals(request.Password?.Trim() ?? string.Empty, request.ConfirmPassword?.Trim() ?? string.Empty, StringComparison.Ordinal))
        {
            result.Add(nameof(request.ConfirmPassword), "La confirmació no coincideix amb la contrasenya.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            result.Add(nameof(request.DisplayName), "El nom visible és obligatori.");
        }
        else if (request.DisplayName.Trim().Length < 3)
        {
            result.Add(nameof(request.DisplayName), "El nom visible ha de tenir com a mínim 3 caràcters.");
        }

        if (string.IsNullOrWhiteSpace(request.City))
        {
            result.Add(nameof(request.City), "La ciutat és obligatòria.");
        }
        else if (request.City.Trim().Length < 2)
        {
            result.Add(nameof(request.City), "La ciutat ha de tenir com a mínim 2 caràcters.");
        }

        if (string.IsNullOrWhiteSpace(request.Country))
        {
            result.Add(nameof(request.Country), "El país és obligatori.");
        }
        else if (request.Country.Trim().Length < 2)
        {
            result.Add(nameof(request.Country), "El país ha de tenir com a mínim 2 caràcters.");
        }

        return result;
    }
}
