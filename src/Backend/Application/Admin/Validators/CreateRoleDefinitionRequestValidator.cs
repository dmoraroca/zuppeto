using System.Text.RegularExpressions;
using YepPet.Application.Validation;

namespace YepPet.Application.Admin.Validators;

public sealed class CreateRoleDefinitionRequestValidator : IValidator<CreateRoleDefinitionRequest>
{
    private static readonly Regex KeyPattern = new("^[A-Za-z][A-Za-z0-9_]{0,31}$", RegexOptions.Compiled);

    public ValidationResult Validate(CreateRoleDefinitionRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.Key))
        {
            result.Add(nameof(request.Key), "Key is required.");
        }
        else
        {
            var key = request.Key.Trim();
            if (key.Length > 32 || !KeyPattern.IsMatch(key))
            {
                result.Add(
                    nameof(request.Key),
                    "Key must start with a letter; only letters, digits and underscore; max 32 characters.");
            }
        }

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
