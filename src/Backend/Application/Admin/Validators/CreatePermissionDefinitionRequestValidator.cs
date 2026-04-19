using YepPet.Application.Validation;

namespace YepPet.Application.Admin.Validators;

public sealed class CreatePermissionDefinitionRequestValidator : IValidator<CreatePermissionDefinitionRequest>
{
    public ValidationResult Validate(CreatePermissionDefinitionRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.Key))
        {
            result.Add(nameof(request.Key), "La clau interna és obligatòria.");
        }
        else
        {
            var key = request.Key.Trim();
            if (key.Length > 160)
            {
                result.Add(nameof(request.Key), "La clau interna és massa llarga (màx. 160 caràcters).");
            }
            else if (!IsValidKey(key))
            {
                result.Add(nameof(request.Key), "La clau ha de tenir format tipus menu.xxx, page.xxx o action.xxx.");
            }
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            result.Add(nameof(request.DisplayName), "El nom visible és obligatori.");
        }
        else if (request.DisplayName.Trim().Length > 160)
        {
            result.Add(nameof(request.DisplayName), "El nom visible és massa llarg (màx. 160 caràcters).");
        }

        if (request.Description is not null && request.Description.Length > 512)
        {
            result.Add(nameof(request.Description), "La descripció és massa llarga (màx. 512 caràcters).");
        }

        if (string.IsNullOrWhiteSpace(request.ScopeType))
        {
            result.Add(nameof(request.ScopeType), "Cal triar el tipus (àmbit) del permís.");
        }
        else if (!IsAllowedScope(request.ScopeType.Trim()))
        {
            result.Add(nameof(request.ScopeType), "El tipus ha de ser menu, page o action.");
        }
        else if (!PermissionScopePayloadRules.TryNormalize(request.ScopeType.Trim(), request.ScopePayload, out _, out var scopePayloadError))
        {
            result.Add(nameof(request.ScopePayload), scopePayloadError);
        }

        return result;
    }

    private static bool IsAllowedScope(string scope)
    {
        return scope.Equals("menu", StringComparison.OrdinalIgnoreCase)
            || scope.Equals("page", StringComparison.OrdinalIgnoreCase)
            || scope.Equals("action", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidKey(string key)
    {
        var firstDot = key.IndexOf('.');
        if (firstDot <= 0 || firstDot == key.Length - 1)
        {
            return false;
        }

        var scope = key[..firstDot];
        if (!IsAllowedScope(scope))
        {
            return false;
        }

        foreach (var ch in key)
        {
            if (char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_')
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
