namespace Zuppeto.Domain.Places;

public enum PlaceMediaSource
{
    PetiLoc,
    Owner,
    User,
    WikimediaCommons
}

public enum PlaceMediaVerificationStatus
{
    Pending,
    Verified,
    Rejected
}

public sealed record PlaceMediaAsset(
    Guid Id,
    Guid PlaceId,
    PlaceMediaSource Source,
    Uri OriginalUri,
    Uri ServedUri,
    string Author,
    string LicenseIdentifier,
    string? LicenseVersion,
    Uri? LicenseUri,
    string Attribution,
    DateTimeOffset RetrievedAtUtc,
    bool PermanentStorageAllowed,
    bool TransformationAllowed,
    string ContentHash,
    PlaceMediaVerificationStatus VerificationStatus,
    bool CanBePrimary,
    bool IsPrimary,
    string? WikidataQid,
    string? WikimediaFileName,
    Uri? EvidenceUri,
    string? RejectionReason);

public sealed class PlaceMediaSelectionPolicy
{
    public PlaceMediaAsset? SelectPrimary(IEnumerable<PlaceMediaAsset> assets)
    {
        var eligible = assets
            .Where(asset => asset.VerificationStatus == PlaceMediaVerificationStatus.Verified)
            .Where(asset => asset.CanBePrimary)
            .ToArray();

        return eligible
            .OrderByDescending(asset => asset.IsPrimary)
            .ThenBy(asset => Priority(asset.Source))
            .ThenByDescending(asset => asset.RetrievedAtUtc)
            .FirstOrDefault();
    }

    private static int Priority(PlaceMediaSource source) => source switch
    {
        PlaceMediaSource.PetiLoc => 0,
        PlaceMediaSource.Owner => 1,
        PlaceMediaSource.User => 2,
        PlaceMediaSource.WikimediaCommons => 3,
        _ => int.MaxValue
    };
}

public sealed record PlaceMediaLicenseDecision(
    bool Allowed,
    bool PermanentStorageAllowed,
    bool TransformationAllowed,
    string? Version,
    string Reason);

public sealed class PlaceMediaLicensePolicy
{
    public PlaceMediaLicenseDecision Evaluate(string identifier, string? licenseUri)
    {
        var normalized = identifier.Trim().ToUpperInvariant().Replace('_', '-');
        if (normalized.Contains("-NC", StringComparison.Ordinal) ||
            normalized.Contains("-ND", StringComparison.Ordinal))
        {
            return Rejected("La llicència no permet ús comercial o transformacions.");
        }

        if (normalized is "CC0" or "CC0-1.0" or "PUBLIC DOMAIN" or "PUBLIC-DOMAIN" or "PDM")
        {
            return new(true, true, true, Version(normalized, licenseUri), "Llicència lliure compatible.");
        }

        if (normalized.StartsWith("CC-BY-", StringComparison.Ordinal) ||
            normalized.StartsWith("CC BY ", StringComparison.Ordinal) ||
            normalized.StartsWith("CC-BY-SA-", StringComparison.Ordinal) ||
            normalized.StartsWith("CC BY-SA ", StringComparison.Ordinal))
        {
            return new(true, true, true, Version(normalized, licenseUri), "Llicència Creative Commons compatible amb ús comercial i transformació.");
        }

        return Rejected("No s’ha pogut demostrar una llicència compatible.");
    }

    private static PlaceMediaLicenseDecision Rejected(string reason) => new(false, false, false, null, reason);

    private static string? Version(string identifier, string? licenseUri)
    {
        foreach (var version in new[] { "4.0", "3.0", "2.5", "2.0", "1.0" })
        {
            if (identifier.Contains(version, StringComparison.Ordinal) ||
                licenseUri?.Contains($"/{version}/", StringComparison.Ordinal) == true)
            {
                return version;
            }
        }

        return null;
    }
}
