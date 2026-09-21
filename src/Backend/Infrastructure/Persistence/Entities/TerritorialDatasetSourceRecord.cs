namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class TerritorialDatasetSourceRecord
{
    public Guid Id { get; set; }
    public Guid CountryId { get; set; }
    public CountryRecord Country { get; set; } = null!;
    public string Organisation { get; set; } = string.Empty;
    public string Dataset { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? DownloadUrl { get; set; }
    public string? License { get; set; }
    public string? LicenseUrl { get; set; }
    public string? Attribution { get; set; }
    public bool? CommercialUseAllowed { get; set; }
    public bool? TransformationAllowed { get; set; }
    public string? Restrictions { get; set; }
    public string? ThirdPartyData { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public UserRecord? VerifiedByUser { get; set; }
    public bool IsActive { get; set; }
    public string PublicationMode { get; set; } = string.Empty;
    public string? DatasetVersion { get; set; }
    public DateOnly? DatasetDate { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<TerritorialUnitRecord> CoordinateUnits { get; set; } = [];
    public ICollection<TerritorialUnitNameRecord> Names { get; set; } = [];
    public ICollection<TerritorialUnitCodeRecord> Codes { get; set; } = [];
    public ICollection<TerritorialLocaleAssignmentRecord> LocaleAssignments { get; set; } = [];
}
