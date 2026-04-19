using System.Collections.Generic;
using System.Text.Json;

namespace YepPet.Application.Admin;

/// <summary>
/// Normalizes and validates JSON stored in <c>permissions.scope_payload</c> for menu/page/action scopes.
/// </summary>
public static class PermissionScopePayloadRules
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static bool TryNormalize(string scopeType, string? rawPayload, out string? normalizedJson, out string errorMessage)
    {
        normalizedJson = null;
        errorMessage = string.Empty;
        var scope = (scopeType ?? string.Empty).Trim().ToLowerInvariant();

        if (scope is "action")
        {
            if (string.IsNullOrWhiteSpace(rawPayload))
            {
                normalizedJson = null;
                return true;
            }

            try
            {
                JsonDocument.Parse(rawPayload);
            }
            catch (JsonException)
            {
                errorMessage = "El contingut d'àmbit no és un JSON vàlid.";
                return false;
            }

            normalizedJson = null;
            return true;
        }

        if (scope is "page")
        {
            if (string.IsNullOrWhiteSpace(rawPayload))
            {
                errorMessage = "Cal indicar una URL per als permisos d'àmbit pàgina.";
                return false;
            }

            ScopePayloadModel? model;
            try
            {
                model = JsonSerializer.Deserialize<ScopePayloadModel>(rawPayload, SerializerOptions);
            }
            catch (JsonException)
            {
                errorMessage = "El contingut d'àmbit no és un JSON vàlid.";
                return false;
            }

            var url = (model?.PageUrl ?? string.Empty).Trim();
            if (url.Length == 0)
            {
                errorMessage = "Cal indicar una URL per als permisos d'àmbit pàgina.";
                return false;
            }

            if (url.Length > 512)
            {
                errorMessage = "La URL és massa llarga (màx. 512 caràcters).";
                return false;
            }

            normalizedJson = JsonSerializer.Serialize(
                new ScopePayloadModel { MenuKeys = new List<string>(), PageUrl = url },
                SerializerOptions);
            return true;
        }

        if (scope is "menu")
        {
            ScopePayloadModel? model;
            if (string.IsNullOrWhiteSpace(rawPayload))
            {
                model = new ScopePayloadModel { MenuKeys = [], PageUrl = null };
            }
            else
            {
                try
                {
                    model = JsonSerializer.Deserialize<ScopePayloadModel>(rawPayload, SerializerOptions);
                }
                catch (JsonException)
                {
                    errorMessage = "El contingut d'àmbit no és un JSON vàlid.";
                    return false;
                }
            }

            var keys = new List<string>();
            if (model?.MenuKeys is not null)
            {
                foreach (var k in model.MenuKeys)
                {
                    var trimmed = (k ?? string.Empty).Trim();
                    if (trimmed.Length == 0)
                    {
                        continue;
                    }

                    if (trimmed.Length > 160)
                    {
                        errorMessage = "Una clau de menú és massa llarga (màx. 160 caràcters).";
                        return false;
                    }

                    keys.Add(trimmed);
                }
            }

            keys.Sort(StringComparer.OrdinalIgnoreCase);
            normalizedJson = JsonSerializer.Serialize(
                new ScopePayloadModel { MenuKeys = keys, PageUrl = null },
                SerializerOptions);
            return true;
        }

        errorMessage = "Tipus d'àmbit no reconegut.";
        return false;
    }

    private sealed class ScopePayloadModel
    {
        public List<string>? MenuKeys { get; set; }

        public string? PageUrl { get; set; }
    }
}
