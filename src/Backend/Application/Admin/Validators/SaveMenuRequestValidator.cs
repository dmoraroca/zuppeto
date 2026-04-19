using System.Text.RegularExpressions;
using YepPet.Application.Validation;

namespace YepPet.Application.Admin.Validators;

public sealed class SaveMenuRequestValidator : IValidator<SaveMenuRequest>
{
    private static readonly Regex RoleKeyPattern = new("^[A-Za-z][A-Za-z0-9_]{0,31}$", RegexOptions.Compiled);

    public ValidationResult Validate(SaveMenuRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.Key))
        {
            result.Add(nameof(request.Key), "Menu key is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Label))
        {
            result.Add(nameof(request.Label), "Menu label is required.");
        }

        if (request.SortOrder < 0)
        {
            result.Add(nameof(request.SortOrder), "Sort order must be zero or greater.");
        }

        if (request.Roles is null)
        {
            result.Add(nameof(request.Roles), "Roles are required.");
        }
        else
        {
            foreach (var role in request.Roles)
            {
                var trimmed = role?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(trimmed))
                {
                    result.Add(nameof(request.Roles), "Each role must be a non-empty key.");
                }
                else if (trimmed.Length > 32 || !RoleKeyPattern.IsMatch(trimmed))
                {
                    result.Add(nameof(request.Roles), $"Role '{role}' is not a valid role key.");
                }
            }
        }

        return result;
    }
}
