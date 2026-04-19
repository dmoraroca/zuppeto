using YepPet.Application.Validation;

namespace YepPet.Application.Admin.Validators;

public sealed class UpdateRoleDefinitionRequestValidator : IValidator<UpdateRoleDefinitionRequest>
{
    public ValidationResult Validate(UpdateRoleDefinitionRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            result.Add(nameof(request.DisplayName), "Display name is required.");
        }
        else if (request.DisplayName.Trim().Length > 120)
        {
            result.Add(nameof(request.DisplayName), "Display name is too long.");
        }

        return result;
    }
}
