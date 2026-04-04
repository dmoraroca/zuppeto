using YepPet.Application.Validation;
using YepPet.Domain.Users;

namespace YepPet.Application.Admin.Validators;

public sealed class UpdateUserRoleRequestValidator : IValidator<UpdateUserRoleRequest>
{
    public ValidationResult Validate(UpdateUserRoleRequest request)
    {
        var result = ValidationResult.Success();

        if (!ValidationHelpers.TryParseEnum<UserRole>(request.Role, out _))
        {
            result.Add(nameof(request.Role), "Role is invalid.");
        }

        return result;
    }
}
