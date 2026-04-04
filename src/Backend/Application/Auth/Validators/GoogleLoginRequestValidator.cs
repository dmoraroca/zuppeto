using YepPet.Application.Validation;

namespace YepPet.Application.Auth.Validators;

public sealed class GoogleLoginRequestValidator : IValidator<GoogleLoginRequest>
{
    public ValidationResult Validate(GoogleLoginRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            result.Add(nameof(request.IdToken), "Id token is required.");
        }

        return result;
    }
}
