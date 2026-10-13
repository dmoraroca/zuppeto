namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class PetFriendlyAssertionRecord
{
    public Guid Id { get; set; }
    public Guid PlaceId { get; set; }
    public PlaceRecord Place { get; set; } = null!;
    public string SpeciesCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ConditionsJson { get; set; } = "[]";
    public string SourceKey { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string ObservedValue { get; set; } = string.Empty;
    public DateTimeOffset ObservedAtUtc { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public DateTimeOffset? ValidUntilUtc { get; set; }
    public string VerificationMethod { get; set; } = string.Empty;
    public string Confidence { get; set; } = string.Empty;
    public Guid? ResponsibleActorId { get; set; }
    public string? EvidenceUri { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public Guid? SupersedesAssertionId { get; set; }
}

public sealed class CommercialChainRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public ICollection<ChainPlaceRecord> Places { get; set; } = [];
    public ICollection<ChainPetPolicyRecord> Policies { get; set; } = [];
}

public sealed class ChainPlaceRecord
{
    public Guid ChainId { get; set; }
    public CommercialChainRecord Chain { get; set; } = null!;
    public Guid PlaceId { get; set; }
    public PlaceRecord Place { get; set; } = null!;
    public bool IsFranchise { get; set; }
    public DateTimeOffset LinkedAtUtc { get; set; }
}

public sealed class ChainPetPolicyRecord
{
    public Guid Id { get; set; }
    public Guid ChainId { get; set; }
    public CommercialChainRecord Chain { get; set; } = null!;
    public string CountryCode { get; set; } = string.Empty;
    public Guid? TerritorialUnitId { get; set; }
    public TerritorialUnitRecord? TerritorialUnit { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SpeciesJson { get; set; } = "[]";
    public string ConditionsJson { get; set; } = "[]";
    public DateTimeOffset ValidFromUtc { get; set; }
    public DateTimeOffset? ValidUntilUtc { get; set; }
    public bool HasApplicabilityEvidence { get; set; }
    public string SourceKey { get; set; } = string.Empty;
    public DateTimeOffset? VerifiedAtUtc { get; set; }
}
