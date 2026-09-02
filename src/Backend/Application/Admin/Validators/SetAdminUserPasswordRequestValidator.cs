using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Admin.Validators;

public sealed class SetAdminUserPasswordRequestValidator : IValidator<SetAdminUserPasswordRequest>
{
    public ValidationResult Validate(SetAdminUserPasswordRequest request)
    {
        var result = ValidationResult.Success();
        var password = request.NewPassword?.Trim() ?? string.Empty;
        var confirm = request.ConfirmNewPassword?.Trim() ?? string.Empty;

        if (password.Length == 0)
        {
            result.Add(nameof(request.NewPassword), "La contrasenya nova és obligatòria.");
        }
        else if (password.Length < 6)
        {
            result.Add(nameof(request.NewPassword), "La contrasenya nova ha de tenir com a mínim 6 caràcters.");
        }

        if (!string.Equals(password, confirm, StringComparison.Ordinal))
        {
            result.Add(nameof(request.ConfirmNewPassword), "La confirmació no coincideix amb la contrasenya nova.");
        }

        return result;
    }
}
