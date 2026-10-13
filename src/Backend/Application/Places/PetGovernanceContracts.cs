using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

public sealed record PetAssertionDto(
    Guid Id, Guid PlaceId, string Species, string Status,
    IReadOnlyCollection<PetConditionDto> Conditions, string SourceKey, string? ExternalId,
    string ObservedValue, DateTimeOffset ObservedAtUtc, DateTimeOffset? VerifiedAtUtc,
    DateTimeOffset? ValidUntilUtc, string VerificationMethod, string Confidence,
    Guid? ResponsibleActorId, string? EvidenceUri, string Origin, string State,
    bool IsExpired, bool IsEffective, bool HasConflict);

public sealed record EffectivePetPolicyDto(
    string Species, string Status, Guid? AssertionId, string SourceKey,
    bool IsVerifiedManual, bool IsInheritedFromChain,
    IReadOnlyCollection<PetConditionDto> Conditions);

public sealed record CreatePetAssertionRequest(
    string Species, string Status, IReadOnlyCollection<PetConditionDto>? Conditions,
    string SourceKey, string? ExternalId, string ObservedValue,
    DateTimeOffset ObservedAtUtc, DateTimeOffset? ValidUntilUtc,
    string Origin = "Manual", string Confidence = "Unverified");

public sealed record VerifyPetAssertionRequest(string VerificationMethod, string EvidenceUri, string Reason);
public sealed record DecidePetAssertionRequest(string Action, string Reason);
public sealed record ResolvePetConflictRequest(Guid SelectedAssertionId, string VerificationMethod, string EvidenceUri, string Reason);

public sealed record CommercialChainDto(Guid Id, string Name, DateTimeOffset CreatedAtUtc);
public sealed record CreateCommercialChainRequest(string Name);
public sealed record PlaceChainMembershipDto(Guid ChainId, string ChainName, bool IsFranchise, DateTimeOffset LinkedAtUtc);
public sealed record LinkPlaceToChainRequest(Guid ChainId, bool IsFranchise, string Reason);
public sealed record ChainPetPolicyDto(
    Guid Id, Guid ChainId, string ChainName, string CountryCode, Guid? TerritorialUnitId,
    string Status, IReadOnlyCollection<string> Species, IReadOnlyCollection<PetConditionDto> Conditions,
    DateTimeOffset ValidFromUtc, DateTimeOffset? ValidUntilUtc, bool HasApplicabilityEvidence,
    string SourceKey, string EvidenceUri, DateTimeOffset? VerifiedAtUtc, bool IsCurrent);
public sealed record CreateChainPetPolicyRequest(
    string CountryCode, Guid? TerritorialUnitId, string Status,
    IReadOnlyCollection<string> Species, IReadOnlyCollection<PetConditionDto>? Conditions,
    DateTimeOffset ValidFromUtc, DateTimeOffset? ValidUntilUtc,
    bool HasApplicabilityEvidence, string SourceKey, string EvidenceUri, string Reason);
public sealed record CreateLocalChainExceptionRequest(
    Guid ChainId, string Species, string Status, IReadOnlyCollection<PetConditionDto>? Conditions,
    string EvidenceUri, string Reason, DateTimeOffset? ValidUntilUtc);

public sealed record PetGovernanceDecisionDto(
    Guid Id, Guid? PlaceId, string SubjectType, Guid SubjectId, string Action,
    string Reason, Guid ActorId, DateTimeOffset DecidedAtUtc);

public sealed record PetGovernanceOverviewDto(
    Guid PlaceId, IReadOnlyCollection<PetAssertionDto> Assertions,
    IReadOnlyCollection<EffectivePetPolicyDto> EffectivePolicies,
    IReadOnlyCollection<PlaceChainMembershipDto> ChainMemberships,
    IReadOnlyCollection<ChainPetPolicyDto> ChainPolicies,
    IReadOnlyCollection<PetGovernanceDecisionDto> History,
    bool HasConflicts, bool HasPendingReview, bool HasExpiredData, bool HasUnknownData);

public sealed record ChainPlaceLink(Guid ChainId, Guid PlaceId, bool IsFranchise, DateTimeOffset LinkedAtUtc);

public interface IPetGovernanceRepository
{
    Task<bool> PlaceExistsAsync(Guid placeId, CancellationToken ct);
    Task<IReadOnlyCollection<PetFriendlyAssertion>> ListAssertionsAsync(Guid placeId, CancellationToken ct);
    Task<PetFriendlyAssertion?> GetAssertionAsync(Guid assertionId, CancellationToken ct);
    Task AddAssertionAsync(PetFriendlyAssertion assertion, CancellationToken ct);
    Task UpdateAssertionAsync(PetFriendlyAssertion assertion, CancellationToken ct);
    Task<IReadOnlyCollection<CommercialChainDto>> ListChainsAsync(CancellationToken ct);
    Task<CommercialChainDto?> GetChainAsync(Guid chainId, CancellationToken ct);
    Task AddChainAsync(CommercialChain chain, DateTimeOffset createdAtUtc, CancellationToken ct);
    Task<IReadOnlyCollection<PlaceChainMembershipDto>> ListMembershipsAsync(Guid placeId, CancellationToken ct);
    Task UpsertMembershipAsync(ChainPlaceLink link, CancellationToken ct);
    Task<IReadOnlyCollection<ChainPetPolicyDto>> ListChainPoliciesAsync(Guid? chainId, Guid? placeId, DateTimeOffset atUtc, CancellationToken ct);
    Task AddChainPolicyAsync(ChainPetPolicy policy, string evidenceUri, CancellationToken ct);
    Task AddDecisionAsync(PetGovernanceDecision decision, CancellationToken ct);
    Task<IReadOnlyCollection<PetGovernanceDecisionDto>> ListDecisionsAsync(Guid? placeId, CancellationToken ct);
    Task CommitAsync(CancellationToken ct);
}

public interface IPetGovernanceApplicationService
{
    Task<PetGovernanceOverviewDto> GetOverviewAsync(Guid placeId, CancellationToken ct = default);
    Task<IReadOnlyCollection<PetGovernanceDecisionDto>> ListHistoryAsync(Guid? placeId = null, CancellationToken ct = default);
    Task<PetAssertionDto> AddAssertionAsync(Guid placeId, CreatePetAssertionRequest request, Guid actorId, CancellationToken ct = default);
    Task VerifyAssertionAsync(Guid placeId, Guid assertionId, VerifyPetAssertionRequest request, Guid actorId, CancellationToken ct = default);
    Task DecideAssertionAsync(Guid placeId, Guid assertionId, DecidePetAssertionRequest request, Guid actorId, CancellationToken ct = default);
    Task ResolveConflictAsync(Guid placeId, ResolvePetConflictRequest request, Guid actorId, CancellationToken ct = default);
    Task<IReadOnlyCollection<CommercialChainDto>> ListChainsAsync(CancellationToken ct = default);
    Task<CommercialChainDto> CreateChainAsync(CreateCommercialChainRequest request, Guid actorId, CancellationToken ct = default);
    Task LinkPlaceAsync(Guid placeId, LinkPlaceToChainRequest request, Guid actorId, CancellationToken ct = default);
    Task<ChainPetPolicyDto> AddChainPolicyAsync(Guid chainId, CreateChainPetPolicyRequest request, Guid actorId, CancellationToken ct = default);
    Task<PetAssertionDto> AddLocalExceptionAsync(Guid placeId, CreateLocalChainExceptionRequest request, Guid actorId, CancellationToken ct = default);
}
