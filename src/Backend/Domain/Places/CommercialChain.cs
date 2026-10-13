using Zuppeto.Domain.Common;
using Zuppeto.Domain.Places.ValueObjects;

namespace Zuppeto.Domain.Places;

public sealed class CommercialChain : AggregateRoot<Guid>
{
    public CommercialChain(Guid id, string name) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainRuleException("El nom de la cadena és obligatori.");
        Name = name.Trim();
    }
    public string Name { get; }
}

public sealed record ChainTerritorialScope(string CountryCode, Guid? TerritorialUnitId = null);

public sealed class ChainPetPolicy : Entity<Guid>
{
    public ChainPetPolicy(Guid id, Guid chainId, PetPolicy policy, ChainTerritorialScope scope,
        DateTimeOffset validFromUtc, DateTimeOffset? validUntilUtc, bool applicabilityEvidence,
        string sourceKey, DateTimeOffset? verifiedAtUtc) : base(id)
    {
        ChainId = chainId;
        Policy = policy;
        Scope = scope;
        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
        HasApplicabilityEvidence = applicabilityEvidence;
        SourceKey = sourceKey;
        VerifiedAtUtc = verifiedAtUtc;
    }
    public Guid ChainId { get; }
    public PetPolicy Policy { get; }
    public ChainTerritorialScope Scope { get; }
    public DateTimeOffset ValidFromUtc { get; }
    public DateTimeOffset? ValidUntilUtc { get; }
    public bool HasApplicabilityEvidence { get; }
    public string SourceKey { get; }
    public DateTimeOffset? VerifiedAtUtc { get; }
    public bool AppliesAt(DateTimeOffset atUtc) => HasApplicabilityEvidence && ValidFromUtc <= atUtc && (ValidUntilUtc is null || ValidUntilUtc > atUtc);
}

public sealed class ChainPetPolicyResolver
{
    public PetPolicy Resolve(PetPolicy? verifiedLocalPolicy, ChainPetPolicy? chainPolicy, DateTimeOffset atUtc) =>
        verifiedLocalPolicy ?? (chainPolicy?.AppliesAt(atUtc) == true ? chainPolicy.Policy : Unknown);

    private static PetPolicy Unknown => new(PetAccessStatus.Unknown, [], [], "", "");
}
