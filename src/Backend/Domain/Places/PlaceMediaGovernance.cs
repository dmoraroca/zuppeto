namespace Zuppeto.Domain.Places;

public enum PlaceMediaSource
{
    PetiLoc,
    Owner,
    User,
    WikimediaCommons,
    Mapillary,
    OfficialWebsite
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
    string SourceOrganisation,
    string ExternalId,
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
    bool IdentityVerified,
    string ReuseConditions,
    string? WikidataQid,
    string? WikimediaFileName,
    Uri? EvidenceUri,
    string? RejectionReason,
    DateTimeOffset? CapturedAtUtc = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    decimal? HeadingDegrees = null,
    int? DistanceMeters = null,
    DateTimeOffset? LastCheckedAtUtc = null,
    DateTimeOffset? ModeratedAtUtc = null,
    Guid? ModeratorActorId = null);

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
        PlaceMediaSource.Mapillary => 4,
        PlaceMediaSource.OfficialWebsite => 5,
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

        if (normalized is "CC-BY" or "CC-BY-SA" ||
            normalized.StartsWith("CC-BY-", StringComparison.Ordinal) ||
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
