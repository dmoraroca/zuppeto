using YepPet.Application.Validation;

namespace YepPet.Application.Admin.Validators;

public sealed class UpdateUserRoleRequestValidator : IValidator<UpdateUserRoleRequest>
{
    public ValidationResult Validate(UpdateUserRoleRequest request)
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

        return result;
    }
}
