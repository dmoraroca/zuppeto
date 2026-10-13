using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ValueObjects;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

internal sealed class PetGovernanceRepository(ZuppetoDbContext db) : IPetGovernanceRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Task<bool> PlaceExistsAsync(Guid placeId, CancellationToken ct) => db.Places.AnyAsync(x => x.Id == placeId, ct);

    public async Task<IReadOnlyCollection<PetFriendlyAssertion>> ListAssertionsAsync(Guid placeId, CancellationToken ct) =>
        (await db.PetFriendlyAssertions.AsNoTracking().Where(x => x.PlaceId == placeId)
            .OrderByDescending(x => x.ObservedAtUtc).ToArrayAsync(ct)).Select(ToDomain).ToArray();

    public async Task<PetFriendlyAssertion?> GetAssertionAsync(Guid assertionId, CancellationToken ct)
    {
        var row = await db.PetFriendlyAssertions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == assertionId, ct);
        return row is null ? null : ToDomain(row);
    }

    public Task AddAssertionAsync(PetFriendlyAssertion assertion, CancellationToken ct)
    { db.PetFriendlyAssertions.Add(ToRecord(assertion)); return Task.CompletedTask; }

    public async Task UpdateAssertionAsync(PetFriendlyAssertion assertion, CancellationToken ct)
    {
        var row = await db.PetFriendlyAssertions.SingleAsync(x => x.Id == assertion.Id, ct);
        Apply(assertion, row);
    }

    public async Task<IReadOnlyCollection<CommercialChainDto>> ListChainsAsync(CancellationToken ct) =>
        await db.CommercialChains.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new CommercialChainDto(x.Id, x.Name, x.CreatedAtUtc)).ToArrayAsync(ct);

    public Task<CommercialChainDto?> GetChainAsync(Guid chainId, CancellationToken ct) =>
        db.CommercialChains.AsNoTracking().Where(x => x.Id == chainId)
            .Select(x => new CommercialChainDto(x.Id, x.Name, x.CreatedAtUtc)).SingleOrDefaultAsync(ct);

    public Task AddChainAsync(CommercialChain chain, DateTimeOffset createdAtUtc, CancellationToken ct)
    { db.CommercialChains.Add(new() { Id = chain.Id, Name = chain.Name, CreatedAtUtc = createdAtUtc }); return Task.CompletedTask; }

    public async Task<IReadOnlyCollection<PlaceChainMembershipDto>> ListMembershipsAsync(Guid placeId, CancellationToken ct) =>
        await db.ChainPlaces.AsNoTracking().Where(x => x.PlaceId == placeId).OrderBy(x => x.Chain.Name)
            .Select(x => new PlaceChainMembershipDto(x.ChainId, x.Chain.Name, x.IsFranchise, x.LinkedAtUtc)).ToArrayAsync(ct);

    public async Task UpsertMembershipAsync(ChainPlaceLink link, CancellationToken ct)
    {
        var row = await db.ChainPlaces.SingleOrDefaultAsync(x => x.ChainId == link.ChainId && x.PlaceId == link.PlaceId, ct);
        if (row is null) db.ChainPlaces.Add(new() { ChainId = link.ChainId, PlaceId = link.PlaceId, IsFranchise = link.IsFranchise, LinkedAtUtc = link.LinkedAtUtc });
        else { row.IsFranchise = link.IsFranchise; row.LinkedAtUtc = link.LinkedAtUtc; }
    }

    public async Task<IReadOnlyCollection<ChainPetPolicyDto>> ListChainPoliciesAsync(Guid? chainId, Guid? placeId, DateTimeOffset atUtc, CancellationToken ct)
    {
        var query = db.ChainPetPolicies.AsNoTracking().AsQueryable();
        if (chainId is not null) query = query.Where(x => x.ChainId == chainId);
        if (placeId is not null)
        {
            query = query.Where(x => db.ChainPlaces.Any(link => link.PlaceId == placeId && link.ChainId == x.ChainId));
            var place = await db.Places.AsNoTracking().Where(x => x.Id == placeId)
                .Select(x => new { x.TerritorialCountry, x.TerritorialUnitId }).SingleAsync(ct);
            var iso2 = place.TerritorialCountry == null ? null : place.TerritorialCountry.Iso2;
            if (!string.IsNullOrWhiteSpace(iso2)) query = query.Where(x => x.CountryCode == iso2 && (x.TerritorialUnitId == null || x.TerritorialUnitId == place.TerritorialUnitId));
            else query = query.Where(x => false);
        }
        var rows = await query.OrderByDescending(x => x.ValidFromUtc).Select(x => new
        {
            x.Id, x.ChainId, ChainName = x.Chain.Name, x.CountryCode, x.TerritorialUnitId, x.Status,
            x.SpeciesJson, x.ConditionsJson, x.ValidFromUtc, x.ValidUntilUtc, x.HasApplicabilityEvidence,
            x.SourceKey, x.EvidenceUri, x.VerifiedAtUtc
        }).ToArrayAsync(ct);
        return rows.Select(x => new ChainPetPolicyDto(x.Id, x.ChainId, x.ChainName, x.CountryCode, x.TerritorialUnitId,
            x.Status, ReadStrings(x.SpeciesJson), ReadConditions(x.ConditionsJson).Select(ToDto).ToArray(),
            x.ValidFromUtc, x.ValidUntilUtc, x.HasApplicabilityEvidence, x.SourceKey, x.EvidenceUri, x.VerifiedAtUtc,
            x.HasApplicabilityEvidence && x.ValidFromUtc <= atUtc && (x.ValidUntilUtc is null || x.ValidUntilUtc > atUtc))).ToArray();
    }

    public Task AddChainPolicyAsync(ChainPetPolicy policy, string evidenceUri, CancellationToken ct)
    {
        db.ChainPetPolicies.Add(new()
        {
            Id = policy.Id, ChainId = policy.ChainId, CountryCode = policy.Scope.CountryCode,
            TerritorialUnitId = policy.Scope.TerritorialUnitId, Status = policy.Policy.Status.ToString(),
            SpeciesJson = JsonSerializer.Serialize(policy.Policy.Species.Select(x => x.Code)),
            ConditionsJson = JsonSerializer.Serialize(policy.Policy.Conditions.Select(ToDto)),
            ValidFromUtc = policy.ValidFromUtc, ValidUntilUtc = policy.ValidUntilUtc,
            HasApplicabilityEvidence = policy.HasApplicabilityEvidence, SourceKey = policy.SourceKey,
            EvidenceUri = evidenceUri, VerifiedAtUtc = policy.VerifiedAtUtc
        });
        return Task.CompletedTask;
    }

    public Task AddDecisionAsync(PetGovernanceDecision x, CancellationToken ct)
    {
        db.PetGovernanceDecisions.Add(new() { Id = x.Id, PlaceId = x.PlaceId, SubjectType = x.SubjectType,
            SubjectId = x.SubjectId, Action = x.Action, Reason = x.Reason, ActorId = x.ActorId,
            DecidedAtUtc = x.DecidedAtUtc, BeforeSnapshot = x.BeforeSnapshot, AfterSnapshot = x.AfterSnapshot });
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<PetGovernanceDecisionDto>> ListDecisionsAsync(Guid? placeId, CancellationToken ct) =>
        await db.PetGovernanceDecisions.AsNoTracking().Where(x => placeId == null || x.PlaceId == placeId)
            .OrderByDescending(x => x.DecidedAtUtc).Select(x => new PetGovernanceDecisionDto(x.Id, x.PlaceId,
                x.SubjectType, x.SubjectId, x.Action, x.Reason, x.ActorId, x.DecidedAtUtc)).ToArrayAsync(ct);

    public Task CommitAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    private static PetFriendlyAssertion ToDomain(PetFriendlyAssertionRecord x) => new(x.Id, x.PlaceId,
        new PetSpecies(x.SpeciesCode), Parse<PetAccessStatus>(x.Status), ReadConditions(x.ConditionsJson), x.SourceKey,
        x.ExternalId, x.ObservedValue, x.ObservedAtUtc, Parse<PetAssertionOrigin>(x.Origin), Parse<PetConfidence>(x.Confidence),
        Parse<PetVerificationMethod>(x.VerificationMethod), x.VerifiedAtUtc, x.ResponsibleActorId, x.EvidenceUri,
        x.ValidUntilUtc, Parse<PetAssertionState>(x.State));

    private static PetFriendlyAssertionRecord ToRecord(PetFriendlyAssertion x)
    { var row = new PetFriendlyAssertionRecord { Id = x.Id, PlaceId = x.PlaceId }; Apply(x, row); return row; }
    private static void Apply(PetFriendlyAssertion x, PetFriendlyAssertionRecord row)
    {
        row.SpeciesCode = x.Species.Code; row.Status = x.Status.ToString();
        row.ConditionsJson = JsonSerializer.Serialize(x.Conditions.Select(ToDto)); row.SourceKey = x.SourceKey;
        row.ExternalId = x.ExternalId; row.ObservedValue = x.ObservedValue; row.ObservedAtUtc = x.ObservedAtUtc;
        row.VerifiedAtUtc = x.VerifiedAtUtc; row.ValidUntilUtc = x.ValidUntilUtc;
        row.VerificationMethod = x.VerificationMethod.ToString(); row.Confidence = x.Confidence.ToString();
        row.ResponsibleActorId = x.ResponsibleActorId; row.EvidenceUri = x.EvidenceUri;
        row.Origin = x.Origin.ToString(); row.State = x.State.ToString();
    }
    private static T Parse<T>(string value) where T : struct, Enum => Enum.Parse<T>(value, true);
    private static string[] ReadStrings(string json) => JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
    private static PetAccessCondition[] ReadConditions(string json) =>
        (JsonSerializer.Deserialize<PetConditionDto[]>(json, JsonOptions) ?? []).Select(x => new PetAccessCondition(x.Code, x.Value)).ToArray();
    private static PetConditionDto ToDto(PetAccessCondition x) => new(x.Code, x.Value);
}
