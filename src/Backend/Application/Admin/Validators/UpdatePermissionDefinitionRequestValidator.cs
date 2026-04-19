using YepPet.Application.Validation;

namespace YepPet.Application.Admin.Validators;

public sealed class UpdatePermissionDefinitionRequestValidator : IValidator<UpdatePermissionDefinitionRequest>
{
    public ValidationResult Validate(UpdatePermissionDefinitionRequest request)
    {
        var result = ValidationResult.Success();

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
}
