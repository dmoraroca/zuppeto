using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Admin.Validators;

public sealed class UpdateRolePermissionsRequestValidator : IValidator<UpdateRolePermissionsRequest>
{
    public ValidationResult Validate(UpdateRolePermissionsRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            result.Add(nameof(request.Role), "El rol és obligatori.");
        }

        if (request.PermissionKeys is null)
        {
            result.Add(nameof(request.PermissionKeys), "Cal indicar les claus de permís.");
        }

        return result;
    }
}
