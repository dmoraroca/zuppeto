namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class PlaceMediaAssetRecord
{
    public Guid Id { get; set; }
    public Guid PlaceId { get; set; }
    public PlaceRecord Place { get; set; } = null!;
    public string Source { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public string ServedUrl { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string LicenseIdentifier { get; set; } = string.Empty;
    public string? LicenseVersion { get; set; }
    public string? LicenseUrl { get; set; }
    public string Attribution { get; set; } = string.Empty;
    public DateTimeOffset RetrievedAtUtc { get; set; }
    public bool PermanentStorageAllowed { get; set; }
    public bool TransformationAllowed { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public bool CanBePrimary { get; set; }
    public bool IsPrimary { get; set; }
    public string? WikidataQid { get; set; }
    public string? WikimediaFileName { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
