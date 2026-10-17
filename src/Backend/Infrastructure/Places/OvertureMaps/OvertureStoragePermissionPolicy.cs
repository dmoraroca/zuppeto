using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.OvertureMaps;

internal sealed class OvertureStoragePermissionPolicy : IPlaceStoragePermissionPolicy
{
    internal const string License = "CDLA-Permissive-2.0+Apache-2.0+CC0-1.0";

    public Result<StoragePermissionDecision> Evaluate(PlaceSourceDescriptor source, string fieldName)
    {
        if (!string.Equals(source.SourceKey, OvertureMapsSource.SourceKey, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(source.LicenseIdentifier, License, StringComparison.Ordinal)
            || !source.PermanentStorageAllowed)
            return Result<StoragePermissionDecision>.Fail(FailureKind.Invalid,
                "La font o el conjunt de llicències no estan aprovats per al connector Overture Maps Places.");
        return Result<StoragePermissionDecision>.Success(new(true,
            $"Camp {fieldName} persistent amb procedència, versió i llicència Overture conservades.",
            DateTimeOffset.UtcNow));
    }
}
