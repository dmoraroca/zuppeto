using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Admin.Validators;

public sealed class UpdateUserRoleRequestValidator : IValidator<UpdateUserRoleRequest>
{
    public ValidationResult Validate(UpdateUserRoleRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            result.Add(nameof(request.Role), "El rol és obligatori.");
        }

        return result;
    }
}
