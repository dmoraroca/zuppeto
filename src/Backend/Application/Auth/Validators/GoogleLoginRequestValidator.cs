using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Auth.Validators;

public sealed class GoogleLoginRequestValidator : IValidator<GoogleLoginRequest>
{
    public ValidationResult Validate(GoogleLoginRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            result.Add(nameof(request.IdToken), "Falta el testimoni d’identitat de Google.");
        }

        return result;
    }
}
