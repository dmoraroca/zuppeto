using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Places;

public enum PetAccessStatus { Allowed, NotAllowed, TerraceOnly, Unknown, PendingReview }
public enum PetAssertionOrigin { Manual, Automatic }
public enum PetConfidence { Unverified, Low, Medium, High, Confirmed }
public enum PetVerificationMethod
{
    Unknown, OwnerStatement, StaffStatement, OnSite, UserReport,
    OfficialWebsite, OfficialDataset, ExternalDataset
}
public enum PetAssertionState { Current, Superseded, Questioned, Revoked }

public sealed record PetSpecies
{
    public static readonly PetSpecies Dog = new("dog");
    public static readonly PetSpecies Cat = new("cat");

    public PetSpecies(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainRuleException("El codi d'espècie és obligatori.");
        Code = code.Trim().ToLowerInvariant();
    }

    public string Code { get; }
}

public sealed record PetAccessCondition
{
    public PetAccessCondition(string code, string? value = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainRuleException("El codi de condició és obligatori.");
        Code = code.Trim().ToLowerInvariant();
        Value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public string Code { get; }
    public string? Value { get; }
}

public sealed class PetFriendlyAssertion : Entity<Guid>
{
    public PetFriendlyAssertion(
        Guid id, Guid placeId, PetSpecies species, PetAccessStatus status,
        IEnumerable<PetAccessCondition> conditions, string sourceKey, string? externalId,
        string observedValue, DateTimeOffset observedAtUtc, PetAssertionOrigin origin,
        PetConfidence confidence, PetVerificationMethod verificationMethod = PetVerificationMethod.Unknown,
        DateTimeOffset? verifiedAtUtc = null, Guid? responsibleActorId = null,
        string? evidenceUri = null, DateTimeOffset? validUntilUtc = null,
        PetAssertionState state = PetAssertionState.Current) : base(id)
    {
        if (string.IsNullOrWhiteSpace(sourceKey)) throw new DomainRuleException("La font és obligatòria.");
        if (string.IsNullOrWhiteSpace(observedValue)) throw new DomainRuleException("El valor observat és obligatori.");
        if (origin == PetAssertionOrigin.Automatic && verifiedAtUtc is not null && responsibleActorId is null)
            throw new DomainRuleException("Una verificació automàtica no es pot presentar com a verificació humana.");
        PlaceId = placeId;
        Species = species;
        Status = status;
        Conditions = conditions.Distinct().ToArray();
        SourceKey = sourceKey.Trim();
        ExternalId = string.IsNullOrWhiteSpace(externalId) ? null : externalId.Trim();
        ObservedValue = observedValue.Trim();
        ObservedAtUtc = observedAtUtc;
        Origin = origin;
        Confidence = confidence;
        VerificationMethod = verificationMethod;
        VerifiedAtUtc = verifiedAtUtc;
        ResponsibleActorId = responsibleActorId;
        EvidenceUri = string.IsNullOrWhiteSpace(evidenceUri) ? null : evidenceUri.Trim();
        ValidUntilUtc = validUntilUtc;
        State = state;
    }

    public Guid PlaceId { get; }
    public PetSpecies Species { get; }
    public PetAccessStatus Status { get; }
    public IReadOnlyCollection<PetAccessCondition> Conditions { get; }
    public string SourceKey { get; }
    public string? ExternalId { get; }
    public string ObservedValue { get; }
    public DateTimeOffset ObservedAtUtc { get; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }
    public DateTimeOffset? ValidUntilUtc { get; }
    public PetVerificationMethod VerificationMethod { get; private set; }
    public PetConfidence Confidence { get; private set; }
    public Guid? ResponsibleActorId { get; private set; }
    public string? EvidenceUri { get; private set; }
    public PetAssertionOrigin Origin { get; }
    public PetAssertionState State { get; private set; }
    public bool IsEffectiveAt(DateTimeOffset instant) =>
        State == PetAssertionState.Current && (ValidUntilUtc is null || ValidUntilUtc > instant);

    public void Verify(Guid actorId, PetVerificationMethod method, string evidenceUri, DateTimeOffset verifiedAtUtc)
    {
        if (actorId == Guid.Empty) throw new DomainRuleException("L'actor de verificació és obligatori.");
        if (method == PetVerificationMethod.Unknown) throw new DomainRuleException("El mètode de verificació és obligatori.");
        if (string.IsNullOrWhiteSpace(evidenceUri)) throw new DomainRuleException("L'evidència és obligatòria.");
        if (State is PetAssertionState.Revoked or PetAssertionState.Superseded)
            throw new DomainRuleException("No es pot verificar una afirmació revocada o substituïda.");
        VerifiedAtUtc = verifiedAtUtc;
        VerificationMethod = method;
        Confidence = PetConfidence.Confirmed;
        ResponsibleActorId = actorId;
        EvidenceUri = evidenceUri.Trim();
        State = PetAssertionState.Current;
    }

    public void Question(Guid actorId)
    {
        if (actorId == Guid.Empty) throw new DomainRuleException("L'actor és obligatori.");
        ResponsibleActorId = actorId;
        State = PetAssertionState.Questioned;
    }

    public void Revoke(Guid actorId)
    {
        if (actorId == Guid.Empty) throw new DomainRuleException("L'actor és obligatori.");
        ResponsibleActorId = actorId;
        VerifiedAtUtc = null;
        Confidence = PetConfidence.Unverified;
        State = PetAssertionState.Revoked;
    }

    public void Supersede(Guid actorId)
    {
        if (actorId == Guid.Empty) throw new DomainRuleException("L'actor és obligatori.");
        ResponsibleActorId = actorId;
        State = PetAssertionState.Superseded;
    }
}

public sealed class PetAssertionPrecedencePolicy
{
    public PetFriendlyAssertion? SelectEffective(IEnumerable<PetFriendlyAssertion> assertions, DateTimeOffset atUtc) =>
        assertions.Where(x => x.IsEffectiveAt(atUtc))
            .OrderByDescending(Rank)
            .ThenByDescending(x => x.VerifiedAtUtc ?? x.ObservedAtUtc)
            .FirstOrDefault();

    private static int Rank(PetFriendlyAssertion assertion) => assertion switch
    {
        { Origin: PetAssertionOrigin.Manual, VerifiedAtUtc: not null } => 300,
        { Origin: PetAssertionOrigin.Manual } => 200,
        _ => 100
    };
}
