using YepPet.Application.Validation;

namespace YepPet.Application.Auth.Validators;

public sealed class LoginRequestValidator : IValidator<LoginRequest>
{
    public ValidationResult Validate(LoginRequest request)
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

        return result;
    }
}
