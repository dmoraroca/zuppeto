using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.MunicipalOpenData;

internal sealed class DibaMunicipalStoragePermissionPolicy : IPlaceStoragePermissionPolicy
{
    private static readonly HashSet<string> ProhibitedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "rao_social_nom", "rao_social_nif", "legal.name", "tax.id"
    };

    public Result<StoragePermissionDecision> Evaluate(PlaceSourceDescriptor source, string fieldName)
    {
        if (!string.Equals(source.SourceKey, DibaMunicipalSource.SourceKey, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(source.LicenseIdentifier, "CC-BY-4.0", StringComparison.Ordinal)
            || !source.PermanentStorageAllowed)
            return Result<StoragePermissionDecision>.Fail(FailureKind.Invalid,
                "La font o la llicència no estan aprovades per al connector municipal DIBA.");
        if (ProhibitedFields.Contains(fieldName))
            return Result<StoragePermissionDecision>.Success(new(false,
                "El connector no persisteix raó social ni NIF: no són necessaris per al catàleg públic.", DateTimeOffset.UtcNow));
        return Result<StoragePermissionDecision>.Success(new(true,
            $"Camp {fieldName} persistent sota CC BY 4.0 amb atribució i data d’actualització.", DateTimeOffset.UtcNow));
    }
}
