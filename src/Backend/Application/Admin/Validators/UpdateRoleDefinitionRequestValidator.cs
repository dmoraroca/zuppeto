using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Admin.Validators;

public sealed class UpdateRoleDefinitionRequestValidator : IValidator<UpdateRoleDefinitionRequest>
{
    public ValidationResult Validate(UpdateRoleDefinitionRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            result.Add(nameof(request.DisplayName), "Display name is required.");
        }

        return result;
    }
}
