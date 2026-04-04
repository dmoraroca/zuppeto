using YepPet.Application.Validation;
using YepPet.Domain.Users;

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

        if (!ValidationHelpers.TryParseEnum<UserRole>(request.Role, out _))
        {
            result.Add(nameof(request.Role), "Role is invalid.");
        }

        return result;
    }
}
