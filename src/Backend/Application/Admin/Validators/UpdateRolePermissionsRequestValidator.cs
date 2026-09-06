using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Admin.Validators;

public sealed class UpdateRolePermissionsRequestValidator : IValidator<UpdateRolePermissionsRequest>
{
    public ValidationResult Validate(UpdateRolePermissionsRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            result.Add(nameof(request.Role), "Role is required.");
        }

        if (request.PermissionKeys is null)
        {
            result.Add(nameof(request.PermissionKeys), "Permission keys are required.");
        }

        return result;
    }
}
