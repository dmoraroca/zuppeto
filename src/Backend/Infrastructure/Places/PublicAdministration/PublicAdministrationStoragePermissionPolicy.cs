using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.PublicAdministration;

internal sealed class PublicAdministrationStoragePermissionPolicy : IPlaceStoragePermissionPolicy
{
    private static readonly IReadOnlyDictionary<string, string> ApprovedLicenses =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [CataloniaTourismSource.SourceKey] = "CATALONIA-OPEN-DATA",
            [AragonCommercialSource.SourceKey] = "CC-BY-4.0"
        };

    private static readonly HashSet<string> ProhibitedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "tax.id", "legal.name", "owner.name", "owner.surname"
    };

    public Result<StoragePermissionDecision> Evaluate(PlaceSourceDescriptor source, string fieldName)
    {
        if (!ApprovedLicenses.TryGetValue(source.SourceKey, out var license)
            || !string.Equals(source.LicenseIdentifier, license, StringComparison.Ordinal)
            || !source.PermanentStorageAllowed)
            return Result<StoragePermissionDecision>.Fail(FailureKind.Invalid,
                "La font administrativa o la seva llicència no estan aprovades per persistir dades.");
        if (ProhibitedFields.Contains(fieldName))
            return Result<StoragePermissionDecision>.Success(new(false,
                "PetiLoc no necessita ni persisteix identificadors fiscals o identitat del titular.",
                DateTimeOffset.UtcNow));
        return Result<StoragePermissionDecision>.Success(new(true,
            $"Camp {fieldName} persistent amb procedència, llicència i data de consulta.",
            DateTimeOffset.UtcNow));
    }
}
