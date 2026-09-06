using System.Text.RegularExpressions;
using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Admin.Validators;

public sealed class SaveMenuRequestValidator : IValidator<SaveMenuRequest>
{
    private static readonly Regex RoleKeyPattern = new("^[A-Za-z][A-Za-z0-9_]{0,31}$", RegexOptions.Compiled);

    public ValidationResult Validate(SaveMenuRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.Key))
        {
            result.Add(nameof(request.Key), "La clau del menú és obligatòria.");
        }

        if (string.IsNullOrWhiteSpace(request.Label))
        {
            result.Add(nameof(request.Label), "L’etiqueta del menú és obligatòria.");
        }

        if (request.SortOrder < 0)
        {
            result.Add(nameof(request.SortOrder), "L’ordre ha de ser zero o superior.");
        }

        if (request.Roles is null)
        {
            result.Add(nameof(request.Roles), "Cal indicar els rols.");
        }
        else
        {
            foreach (var role in request.Roles)
            {
                var trimmed = role?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(trimmed))
                {
                    result.Add(nameof(request.Roles), "Cada rol ha de tenir una clau.");
                }
                else
                {
                    var roleKey = trimmed.Length > 32 ? trimmed[..32] : trimmed;
                    if (!RoleKeyPattern.IsMatch(roleKey))
                    {
                        result.Add(nameof(request.Roles), $"El rol «{role}» no és una clau vàlida.");
                    }
                }
            }
        }

        return result;
    }
}
