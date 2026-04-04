using YepPet.Application.Validation;
using YepPet.Domain.Users;

namespace YepPet.Application.Admin.Validators;

public sealed class SaveMenuRequestValidator : IValidator<SaveMenuRequest>
{
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
                if (!ValidationHelpers.TryParseEnum<UserRole>(role, out _))
                {
                    result.Add(nameof(request.Roles), $"Role '{role}' is invalid.");
                }
            }
        }

        return result;
    }
}
