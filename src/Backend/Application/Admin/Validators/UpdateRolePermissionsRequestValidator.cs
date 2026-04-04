using YepPet.Application.Validation;
using YepPet.Domain.Users;

namespace YepPet.Application.Admin.Validators;

public sealed class UpdateRolePermissionsRequestValidator : IValidator<UpdateRolePermissionsRequest>
{
    public ValidationResult Validate(UpdateRolePermissionsRequest request)
    {
        var result = ValidationResult.Success();

        if (!ValidationHelpers.TryParseEnum<UserRole>(request.Role, out _))
        {
            result.Add(nameof(request.Role), "Role is invalid.");
        }

        if (request.PermissionKeys is null)
        {
            result.Add(nameof(request.PermissionKeys), "Permission keys are required.");
        }

        return result;
    }
}
