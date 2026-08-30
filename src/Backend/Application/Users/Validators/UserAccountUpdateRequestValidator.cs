using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Users.Validators;

public sealed class UserAccountUpdateRequestValidator : IValidator<UserAccountUpdateRequest>
{
    public ValidationResult Validate(UserAccountUpdateRequest request)
    {
        var result = ValidationResult.Success();

        if (request.Id == Guid.Empty)
        {
            result.Add(nameof(request.Id), "Cal l’identificador d’usuari.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@', StringComparison.Ordinal))
        {
            result.Add(nameof(request.Email), "Cal un email vàlid.");
        }

        var newPassword = request.NewPassword?.Trim() ?? string.Empty;
        var confirm = request.ConfirmNewPassword?.Trim() ?? string.Empty;

        if (newPassword.Length > 0)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                result.Add(nameof(request.CurrentPassword), "Cal la contrasenya actual per canviar-la.");
            }

            if (newPassword.Length < 6)
            {
                result.Add(nameof(request.NewPassword), "La contrasenya nova ha de tenir com a mínim 6 caràcters.");
            }

            if (!string.Equals(newPassword, confirm, StringComparison.Ordinal))
            {
                result.Add(nameof(request.ConfirmNewPassword), "La confirmació no coincideix amb la contrasenya nova.");
            }
        }
        else if (confirm.Length > 0)
        {
            result.Add(nameof(request.ConfirmNewPassword), "Introdueix també la contrasenya nova per confirmar-la.");
        }

        return result;
    }
}
