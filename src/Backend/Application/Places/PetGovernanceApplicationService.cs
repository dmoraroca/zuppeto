using System.Text.Json;
using Zuppeto.Domain.Common;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ValueObjects;

namespace Zuppeto.Application.Places;

internal sealed class PetGovernanceApplicationService(
    IPetGovernanceRepository repository,
    PetAssertionPrecedencePolicy precedence) : IPetGovernanceApplicationService
{
    public async Task<PetGovernanceOverviewDto> GetOverviewAsync(Guid placeId, CancellationToken ct = default)
    {
        await EnsurePlaceAsync(placeId, ct);
        var now = DateTimeOffset.UtcNow;
        var assertions = (await repository.ListAssertionsAsync(placeId, ct)).ToArray();
        var memberships = await repository.ListMembershipsAsync(placeId, ct);
        var chainPolicies = await repository.ListChainPoliciesAsync(null, placeId, now, ct);
        var conflicts = assertions.Where(x => x.IsEffectiveAt(now))
            .GroupBy(x => x.Species.Code)
            .Where(group => group.Select(x => x.Status).Distinct().Count() > 1)
            .SelectMany(x => x).Select(x => x.Id).ToHashSet();

        var effective = new List<EffectivePetPolicyDto>();
        foreach (var speciesGroup in assertions.GroupBy(x => x.Species.Code))
        {
            var selected = precedence.SelectEffective(speciesGroup, now);
            if (selected is not null) effective.Add(ToEffective(selected));
        }
        foreach (var chainPolicy in chainPolicies.Where(x => x.IsCurrent && x.HasApplicabilityEvidence))
        {
            foreach (var species in chainPolicy.Species.Where(species => effective.All(x => x.Species != species)))
                effective.Add(new(species, chainPolicy.Status, null, chainPolicy.SourceKey, false, true, chainPolicy.Conditions));
        }

        return new(placeId, assertions.Select(x => ToDto(x, now, conflicts.Contains(x.Id))).ToArray(), effective,
            memberships, chainPolicies, await repository.ListDecisionsAsync(placeId, ct),
            conflicts.Count > 0,
            assertions.Any(x => x.Status == PetAccessStatus.PendingReview && x.IsEffectiveAt(now)),
            assertions.Any(x => x.ValidUntilUtc <= now),
            assertions.Length == 0 || effective.Count == 0 || effective.Any(x => x.Status == nameof(PetAccessStatus.Unknown)));
    }

    public Task<IReadOnlyCollection<PetGovernanceDecisionDto>> ListHistoryAsync(Guid? placeId = null, CancellationToken ct = default) =>
        repository.ListDecisionsAsync(placeId, ct);

    public async Task<PetAssertionDto> AddAssertionAsync(Guid placeId, CreatePetAssertionRequest request, Guid actorId, CancellationToken ct = default)
    {
        await EnsurePlaceAsync(placeId, ct);
        var assertion = BuildAssertion(placeId, request);
        await repository.AddAssertionAsync(assertion, ct);
        await Audit(placeId, "Assertion", assertion.Id, "Created", "Afirmació creada", actorId, null, assertion, ct);
        await repository.CommitAsync(ct);
        return ToDto(assertion, DateTimeOffset.UtcNow, false);
    }

    public async Task VerifyAssertionAsync(Guid placeId, Guid assertionId, VerifyPetAssertionRequest request, Guid actorId, CancellationToken ct = default)
    {
        var assertion = await GetOwnedAssertion(placeId, assertionId, ct);
        var before = Snapshot(assertion);
        assertion.Verify(actorId, Parse<PetVerificationMethod>(request.VerificationMethod, "mètode de verificació"), request.EvidenceUri, DateTimeOffset.UtcNow);
        await repository.UpdateAssertionAsync(assertion, ct);
        await Audit(placeId, "Assertion", assertion.Id, "Verified", RequiredReason(request.Reason), actorId, before, assertion, ct);
        await repository.CommitAsync(ct);
    }

    public async Task DecideAssertionAsync(Guid placeId, Guid assertionId, DecidePetAssertionRequest request, Guid actorId, CancellationToken ct = default)
    {
        var assertion = await GetOwnedAssertion(placeId, assertionId, ct);
        var before = Snapshot(assertion);
        switch (request.Action.Trim().ToLowerInvariant())
        {
            case "question": assertion.Question(actorId); break;
            case "revoke": assertion.Revoke(actorId); break;
            case "supersede": assertion.Supersede(actorId); break;
            default: throw new DomainRuleException("L'acció ha de ser question, revoke o supersede.");
        }
        await repository.UpdateAssertionAsync(assertion, ct);
        await Audit(placeId, "Assertion", assertion.Id, request.Action, RequiredReason(request.Reason), actorId, before, assertion, ct);
        await repository.CommitAsync(ct);
    }

    public async Task ResolveConflictAsync(Guid placeId, ResolvePetConflictRequest request, Guid actorId, CancellationToken ct = default)
    {
        var assertions = (await repository.ListAssertionsAsync(placeId, ct)).ToArray();
        var selected = assertions.SingleOrDefault(x => x.Id == request.SelectedAssertionId)
            ?? throw new KeyNotFoundException("No s'ha trobat l'afirmació seleccionada.");
        var before = Snapshot(selected);
        selected.Verify(actorId, Parse<PetVerificationMethod>(request.VerificationMethod, "mètode de verificació"), request.EvidenceUri, DateTimeOffset.UtcNow);
        await repository.UpdateAssertionAsync(selected, ct);
        foreach (var conflicting in assertions.Where(x => x.Id != selected.Id && x.Species == selected.Species && x.State == PetAssertionState.Current && x.Status != selected.Status))
        {
            var conflictBefore = Snapshot(conflicting);
            conflicting.Question(actorId);
            await repository.UpdateAssertionAsync(conflicting, ct);
            await Audit(placeId, "Assertion", conflicting.Id, "QuestionedByConflictResolution", RequiredReason(request.Reason), actorId, conflictBefore, conflicting, ct);
        }
        await Audit(placeId, "Assertion", selected.Id, "ConflictResolved", RequiredReason(request.Reason), actorId, before, selected, ct);
        await repository.CommitAsync(ct);
    }

    public Task<IReadOnlyCollection<CommercialChainDto>> ListChainsAsync(CancellationToken ct = default) => repository.ListChainsAsync(ct);

    public async Task<CommercialChainDto> CreateChainAsync(CreateCommercialChainRequest request, Guid actorId, CancellationToken ct = default)
    {
        var chain = new CommercialChain(Guid.NewGuid(), request.Name);
        var now = DateTimeOffset.UtcNow;
        await repository.AddChainAsync(chain, now, ct);
        await Audit(null, "Chain", chain.Id, "Created", "Cadena creada", actorId, null, chain, ct);
        await repository.CommitAsync(ct);
        return new(chain.Id, chain.Name, now);
    }

    public async Task LinkPlaceAsync(Guid placeId, LinkPlaceToChainRequest request, Guid actorId, CancellationToken ct = default)
    {
        await EnsurePlaceAsync(placeId, ct);
        _ = await repository.GetChainAsync(request.ChainId, ct) ?? throw new KeyNotFoundException("No s'ha trobat la cadena.");
        var link = new ChainPlaceLink(request.ChainId, placeId, request.IsFranchise, DateTimeOffset.UtcNow);
        await repository.UpsertMembershipAsync(link, ct);
        await Audit(placeId, "ChainMembership", request.ChainId, "Linked", RequiredReason(request.Reason), actorId, null, link, ct);
        await repository.CommitAsync(ct);
    }

    public async Task<ChainPetPolicyDto> AddChainPolicyAsync(Guid chainId, CreateChainPetPolicyRequest request, Guid actorId, CancellationToken ct = default)
    {
        var chain = await repository.GetChainAsync(chainId, ct) ?? throw new KeyNotFoundException("No s'ha trobat la cadena.");
        if (!request.HasApplicabilityEvidence) throw new DomainRuleException("La política de cadena necessita evidència d'aplicabilitat.");
        if (string.IsNullOrWhiteSpace(request.EvidenceUri)) throw new DomainRuleException("L'evidència de la política és obligatòria.");
        var policy = new ChainPetPolicy(Guid.NewGuid(), chainId,
            new PetPolicy(Parse<PetAccessStatus>(request.Status, "estat"), request.Species.Select(x => new PetSpecies(x)),
                Conditions(request.Conditions), "", ""),
            new ChainTerritorialScope(request.CountryCode.Trim().ToUpperInvariant(), request.TerritorialUnitId),
            request.ValidFromUtc, request.ValidUntilUtc, true, request.SourceKey.Trim(), DateTimeOffset.UtcNow);
        await repository.AddChainPolicyAsync(policy, request.EvidenceUri.Trim(), ct);
        await Audit(null, "ChainPolicy", policy.Id, "Created", RequiredReason(request.Reason), actorId, null, policy, ct);
        await repository.CommitAsync(ct);
        return new(policy.Id, chainId, chain.Name, policy.Scope.CountryCode, policy.Scope.TerritorialUnitId,
            policy.Policy.Status.ToString(), policy.Policy.Species.Select(x => x.Code).ToArray(),
            policy.Policy.Conditions.Select(ToCondition).ToArray(), policy.ValidFromUtc, policy.ValidUntilUtc,
            policy.HasApplicabilityEvidence, policy.SourceKey, request.EvidenceUri.Trim(), policy.VerifiedAtUtc,
            policy.AppliesAt(DateTimeOffset.UtcNow));
    }

    public async Task<PetAssertionDto> AddLocalExceptionAsync(Guid placeId, CreateLocalChainExceptionRequest request, Guid actorId, CancellationToken ct = default)
    {
        var membership = (await repository.ListMembershipsAsync(placeId, ct)).SingleOrDefault(x => x.ChainId == request.ChainId)
            ?? throw new DomainRuleException("El Place no està vinculat a aquesta cadena.");
        var assertion = BuildAssertion(placeId, new(request.Species, request.Status, request.Conditions,
            $"petiloc:chain-exception:{membership.ChainId}", null, request.Status, DateTimeOffset.UtcNow,
            request.ValidUntilUtc, "Manual", "Confirmed"));
        assertion.Verify(actorId, PetVerificationMethod.OnSite, request.EvidenceUri, DateTimeOffset.UtcNow);
        await repository.AddAssertionAsync(assertion, ct);
        await Audit(placeId, "LocalChainException", assertion.Id, "Created", RequiredReason(request.Reason), actorId, null, assertion, ct);
        await repository.CommitAsync(ct);
        return ToDto(assertion, DateTimeOffset.UtcNow, false);
    }

    private async Task EnsurePlaceAsync(Guid placeId, CancellationToken ct)
    { if (!await repository.PlaceExistsAsync(placeId, ct)) throw new KeyNotFoundException("No s'ha trobat el Place."); }

    private async Task<PetFriendlyAssertion> GetOwnedAssertion(Guid placeId, Guid assertionId, CancellationToken ct)
    {
        var assertion = await repository.GetAssertionAsync(assertionId, ct) ?? throw new KeyNotFoundException("No s'ha trobat l'afirmació.");
        if (assertion.PlaceId != placeId) throw new KeyNotFoundException("L'afirmació no pertany al Place.");
        return assertion;
    }

    private static PetFriendlyAssertion BuildAssertion(Guid placeId, CreatePetAssertionRequest r) => new(
        Guid.NewGuid(), placeId, new PetSpecies(r.Species), Parse<PetAccessStatus>(r.Status, "estat"), Conditions(r.Conditions),
        r.SourceKey, r.ExternalId, r.ObservedValue, r.ObservedAtUtc, Parse<PetAssertionOrigin>(r.Origin, "origen"),
        Parse<PetConfidence>(r.Confidence, "confiança"), validUntilUtc: r.ValidUntilUtc);

    private async Task Audit(Guid? placeId, string subjectType, Guid subjectId, string action, string reason,
        Guid actorId, object? before, object? after, CancellationToken ct) => await repository.AddDecisionAsync(new(
            Guid.NewGuid(), placeId, subjectType, subjectId, action, reason, actorId, DateTimeOffset.UtcNow,
            before is string text ? text : Snapshot(before), Snapshot(after)), ct);

    private static string Snapshot(object? value) => value is null ? "{}" : JsonSerializer.Serialize(value);
    private static string RequiredReason(string value) => string.IsNullOrWhiteSpace(value)
        ? throw new DomainRuleException("El motiu de la decisió és obligatori.") : value.Trim();
    private static T Parse<T>(string value, string field) where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var parsed) ? parsed : throw new DomainRuleException($"El camp {field} no és vàlid.");
    private static IReadOnlyCollection<PetAccessCondition> Conditions(IReadOnlyCollection<PetConditionDto>? values) =>
        values?.Select(x => new PetAccessCondition(x.Code, x.Value)).ToArray() ?? [];
    private static PetConditionDto ToCondition(PetAccessCondition x) => new(x.Code, x.Value);
    private static EffectivePetPolicyDto ToEffective(PetFriendlyAssertion x) => new(x.Species.Code, x.Status.ToString(), x.Id,
        x.SourceKey, x.Origin == PetAssertionOrigin.Manual && x.VerifiedAtUtc is not null, false, x.Conditions.Select(ToCondition).ToArray());
    private static PetAssertionDto ToDto(PetFriendlyAssertion x, DateTimeOffset now, bool conflict) => new(x.Id, x.PlaceId,
        x.Species.Code, x.Status.ToString(), x.Conditions.Select(ToCondition).ToArray(), x.SourceKey, x.ExternalId,
        x.ObservedValue, x.ObservedAtUtc, x.VerifiedAtUtc, x.ValidUntilUtc, x.VerificationMethod.ToString(),
        x.Confidence.ToString(), x.ResponsibleActorId, x.EvidenceUri, x.Origin.ToString(), x.State.ToString(),
        x.ValidUntilUtc <= now, x.IsEffectiveAt(now), conflict);
}
