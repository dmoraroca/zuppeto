namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class PlaceMediaAssetRecord
{
    public Guid Id { get; set; }
    public Guid PlaceId { get; set; }
    public PlaceRecord Place { get; set; } = null!;
    public string Source { get; set; } = string.Empty;
    public string SourceOrganisation { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
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
    public bool IdentityVerified { get; set; }
    public string ReuseConditions { get; set; } = string.Empty;
    public string? WikidataQid { get; set; }
    public string? WikimediaFileName { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset? CapturedAtUtc { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? HeadingDegrees { get; set; }
    public int? DistanceMeters { get; set; }
    public DateTimeOffset? LastCheckedAtUtc { get; set; }
    public DateTimeOffset? ModeratedAtUtc { get; set; }
    public Guid? ModeratorActorId { get; set; }
    public Guid? ContributorUserId { get; set; }
    public string? RightsDeclaration { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<PlaceMediaBlobRecord> Blobs { get; set; } = [];
}

public sealed class PlaceMediaBlobRecord
{
    public Guid Id { get; set; }
    public Guid MediaAssetId { get; set; }
    public PlaceMediaAssetRecord MediaAsset { get; set; } = null!;
    public string Variant { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
    public long SizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class PlaceMediaModerationRecord
{
    public Guid Id { get; set; }
    public Guid MediaAssetId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class PlaceMediaReportRecord
{
    public Guid Id { get; set; }
    public Guid MediaAssetId { get; set; }
    public Guid ReporterUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
}

public sealed class PlaceMediaDiscoveryRecord
{
    public Guid Id { get; set; }
    public Guid PlaceId { get; set; }
    public PlaceRecord Place { get; set; } = null!;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset LastCheckedAtUtc { get; set; }
    public DateTimeOffset NextCheckAtUtc { get; set; }
    public int CandidateCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
