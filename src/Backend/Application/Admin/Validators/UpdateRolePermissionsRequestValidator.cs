using YepPet.Application.Validation;

namespace YepPet.Application.Admin.Validators;

public sealed class UpdateRolePermissionsRequestValidator : IValidator<UpdateRolePermissionsRequest>
{
    public ValidationResult Validate(UpdateRolePermissionsRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            result.Add(nameof(request.Role), "Role is required.");
        }
        else if (request.Role.Trim().Length > 32)
        {
            result.Add(nameof(request.Role), "Role is too long.");
        }

        if (request.PermissionKeys is null)
        {
            result.Add(nameof(request.PermissionKeys), "Permission keys are required.");
        }

        return result;
    }
}
