using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.OpenStreetMap;

internal sealed class OsmStoragePermissionPolicy : IPlaceStoragePermissionPolicy
{
    public Result<StoragePermissionDecision> Evaluate(PlaceSourceDescriptor source, string fieldName)
    {
        if (!string.Equals(source.SourceKey, OpenStreetMapSource.SourceKey, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(source.LicenseIdentifier, "ODbL-1.0", StringComparison.Ordinal)
            || !source.PermanentStorageAllowed)
            return Result<StoragePermissionDecision>.Fail(FailureKind.Invalid, "La font o la llicència no estan aprovades per al connector OSM.");
        return Result<StoragePermissionDecision>.Success(new(true,
            $"Camp {fieldName} persistent amb procedència ODbL i atribució obligatòria.", DateTimeOffset.UtcNow));
    }
}
