using Zuppeto.Application.Places;
using Zuppeto.Domain.Places;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class PetGovernanceWorkflowTests
{
    [Theory]
    [InlineData("Admin", true, true)]
    [InlineData("Developer", true, false)]
    [InlineData("Admin", false, false)]
    public void Governance_requires_both_admin_role_and_places_permission(string role, bool permission, bool expected)
    {
        Assert.Equal(expected, PetGovernanceAuthorizationPolicy.Allows(Guid.NewGuid(), role, permission));
        Assert.False(PetGovernanceAuthorizationPolicy.Allows(null, role, permission));
    }

    [Fact]
    public async Task Verification_is_audited_and_verified_manual_data_precedes_automatic_conflict()
    {
        var fixture = new Fixture();
        var manual = await fixture.Service.AddAssertionAsync(fixture.PlaceId,
            Request("Allowed", "petiloc:manual", "Manual"), fixture.ActorId);
        _ = await fixture.Service.AddAssertionAsync(fixture.PlaceId,
            Request("NotAllowed", "osm", "Automatic"), fixture.ActorId);

        await fixture.Service.VerifyAssertionAsync(fixture.PlaceId, manual.Id,
            new("OnSite", "https://evidence.test/manual", "Comprovació presencial"), fixture.ActorId);
        var overview = await fixture.Service.GetOverviewAsync(fixture.PlaceId);

        Assert.True(overview.HasConflicts);
        Assert.Equal("Allowed", Assert.Single(overview.EffectivePolicies).Status);
        Assert.True(Assert.Single(overview.EffectivePolicies).IsVerifiedManual);
        Assert.Contains(fixture.Repository.Decisions, x => x.Action == "Verified");
    }

    [Fact]
    public async Task Resolving_a_conflict_keeps_both_sources_and_questions_the_non_selected_assertion()
    {
        var fixture = new Fixture();
        var selected = await fixture.Service.AddAssertionAsync(fixture.PlaceId, Request("Allowed", "petiloc:manual", "Manual"), fixture.ActorId);
        _ = await fixture.Service.AddAssertionAsync(fixture.PlaceId, Request("NotAllowed", "osm", "Automatic"), fixture.ActorId);

        await fixture.Service.ResolveConflictAsync(fixture.PlaceId,
            new(selected.Id, "OwnerStatement", "https://evidence.test/owner", "Confirmat pel local"), fixture.ActorId);
        var overview = await fixture.Service.GetOverviewAsync(fixture.PlaceId);

        Assert.Equal(2, overview.Assertions.Count);
        Assert.False(overview.HasConflicts);
        Assert.Contains(overview.Assertions, x => x.Id != selected.Id && x.State == "Questioned");
        Assert.Contains(overview.History, x => x.Action == "ConflictResolved");
    }

    [Fact]
    public async Task Revoked_and_expired_assertions_do_not_precede_current_data()
    {
        var fixture = new Fixture();
        var manual = await fixture.Service.AddAssertionAsync(fixture.PlaceId, Request("Allowed", "petiloc:manual", "Manual"), fixture.ActorId);
        await fixture.Service.VerifyAssertionAsync(fixture.PlaceId, manual.Id,
            new("OnSite", "https://evidence.test/manual", "Verificada"), fixture.ActorId);
        await fixture.Service.DecideAssertionAsync(fixture.PlaceId, manual.Id, new("revoke", "Revocada pel local"), fixture.ActorId);
        _ = await fixture.Service.AddAssertionAsync(fixture.PlaceId,
            Request("TerraceOnly", "osm", "Automatic", DateTimeOffset.UtcNow.AddMinutes(-1)), fixture.ActorId);

        var overview = await fixture.Service.GetOverviewAsync(fixture.PlaceId);
        Assert.Empty(overview.EffectivePolicies);
        Assert.True(overview.HasExpiredData);
        Assert.Contains(overview.History, x => x.Action == "revoke");
    }

    [Fact]
    public async Task Chain_policy_requires_evidence_and_verified_local_exception_precedes_it()
    {
        var fixture = new Fixture();
        var chain = await fixture.Service.CreateChainAsync(new("Cadena sintètica"), fixture.ActorId);
        await fixture.Service.LinkPlaceAsync(fixture.PlaceId, new(chain.Id, true, "Franquícia confirmada"), fixture.ActorId);

        await Assert.ThrowsAsync<Zuppeto.Domain.Common.DomainRuleException>(() => fixture.Service.AddChainPolicyAsync(chain.Id,
            new("ES", null, "Allowed", ["dog"], [], DateTimeOffset.UtcNow.AddDays(-1), null,
                false, "chain-web", "", "Sense evidència"), fixture.ActorId));

        _ = await fixture.Service.AddChainPolicyAsync(chain.Id,
            new("ES", null, "Allowed", ["dog"], [], DateTimeOffset.UtcNow.AddDays(-1), null,
                true, "chain-web", "https://chain.test/policy", "Política oficial"), fixture.ActorId);
        _ = await fixture.Service.AddLocalExceptionAsync(fixture.PlaceId,
            new(chain.Id, "dog", "NotAllowed", [], "https://place.test/exception", "Excepció del local", null), fixture.ActorId);

        var overview = await fixture.Service.GetOverviewAsync(fixture.PlaceId);
        Assert.Equal("NotAllowed", Assert.Single(overview.EffectivePolicies).Status);
        Assert.False(Assert.Single(overview.EffectivePolicies).IsInheritedFromChain);
        Assert.Contains(overview.History, x => x.SubjectType == "LocalChainException");
        Assert.Contains(await fixture.Service.ListHistoryAsync(), x => x.SubjectType == "ChainPolicy");
    }

    private static CreatePetAssertionRequest Request(string status, string source, string origin, DateTimeOffset? validUntil = null) =>
        new("dog", status, [], source, null, status, DateTimeOffset.UtcNow, validUntil, origin, "Unverified");

    private sealed class Fixture
    {
        public Guid PlaceId { get; } = Guid.NewGuid();
        public Guid ActorId { get; } = Guid.NewGuid();
        public FakeRepository Repository { get; }
        public PetGovernanceApplicationService Service { get; }
        public Fixture()
        {
            Repository = new FakeRepository(PlaceId);
            Service = new(Repository, new PetAssertionPrecedencePolicy());
        }
    }

    private sealed class FakeRepository(Guid placeId) : IPetGovernanceRepository
    {
        private readonly List<PetFriendlyAssertion> assertions = [];
        private readonly List<CommercialChainDto> chains = [];
        private readonly List<PlaceChainMembershipDto> memberships = [];
        private readonly List<ChainPetPolicyDto> policies = [];
        public List<PetGovernanceDecision> Decisions { get; } = [];
        public Task<bool> PlaceExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(id == placeId);
        public Task<IReadOnlyCollection<PetFriendlyAssertion>> ListAssertionsAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<PetFriendlyAssertion>>(assertions.ToArray());
        public Task<PetFriendlyAssertion?> GetAssertionAsync(Guid id, CancellationToken ct) => Task.FromResult(assertions.SingleOrDefault(x => x.Id == id));
        public Task AddAssertionAsync(PetFriendlyAssertion value, CancellationToken ct) { assertions.Add(value); return Task.CompletedTask; }
        public Task UpdateAssertionAsync(PetFriendlyAssertion value, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyCollection<CommercialChainDto>> ListChainsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<CommercialChainDto>>(chains);
        public Task<CommercialChainDto?> GetChainAsync(Guid id, CancellationToken ct) => Task.FromResult(chains.SingleOrDefault(x => x.Id == id));
        public Task AddChainAsync(CommercialChain chain, DateTimeOffset at, CancellationToken ct) { chains.Add(new(chain.Id, chain.Name, at)); return Task.CompletedTask; }
        public Task<IReadOnlyCollection<PlaceChainMembershipDto>> ListMembershipsAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<PlaceChainMembershipDto>>(memberships.Where(x => id == placeId).ToArray());
        public Task UpsertMembershipAsync(ChainPlaceLink link, CancellationToken ct) { memberships.RemoveAll(x => x.ChainId == link.ChainId); memberships.Add(new(link.ChainId, chains.Single(x => x.Id == link.ChainId).Name, link.IsFranchise, link.LinkedAtUtc)); return Task.CompletedTask; }
        public Task<IReadOnlyCollection<ChainPetPolicyDto>> ListChainPoliciesAsync(Guid? chainId, Guid? requestedPlaceId, DateTimeOffset at, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<ChainPetPolicyDto>>(policies.Where(x => chainId is null || x.ChainId == chainId).ToArray());
        public Task AddChainPolicyAsync(ChainPetPolicy policy, string evidence, CancellationToken ct) { policies.Add(new(policy.Id, policy.ChainId, chains.Single(x => x.Id == policy.ChainId).Name, policy.Scope.CountryCode, policy.Scope.TerritorialUnitId, policy.Policy.Status.ToString(), policy.Policy.Species.Select(x => x.Code).ToArray(), policy.Policy.Conditions.Select(x => new PetConditionDto(x.Code, x.Value)).ToArray(), policy.ValidFromUtc, policy.ValidUntilUtc, policy.HasApplicabilityEvidence, policy.SourceKey, evidence, policy.VerifiedAtUtc, policy.AppliesAt(DateTimeOffset.UtcNow))); return Task.CompletedTask; }
        public Task AddDecisionAsync(PetGovernanceDecision decision, CancellationToken ct) { Decisions.Add(decision); return Task.CompletedTask; }
        public Task<IReadOnlyCollection<PetGovernanceDecisionDto>> ListDecisionsAsync(Guid? id, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<PetGovernanceDecisionDto>>(Decisions.Where(x => id == null || x.PlaceId == id).Select(x => new PetGovernanceDecisionDto(x.Id, x.PlaceId, x.SubjectType, x.SubjectId, x.Action, x.Reason, x.ActorId, x.DecidedAtUtc)).ToArray());
        public Task CommitAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
