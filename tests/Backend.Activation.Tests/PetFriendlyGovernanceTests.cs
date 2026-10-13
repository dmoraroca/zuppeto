using Zuppeto.Application.Places;
using Zuppeto.Domain.Places;
using Zuppeto.Domain.Places.ValueObjects;
using Zuppeto.Infrastructure.Persistence.Mappings;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class PetFriendlyGovernanceTests
{
    [Theory]
    [InlineData(PetAccessStatus.Allowed)]
    [InlineData(PetAccessStatus.NotAllowed)]
    [InlineData(PetAccessStatus.TerraceOnly)]
    [InlineData(PetAccessStatus.Unknown)]
    [InlineData(PetAccessStatus.PendingReview)]
    public void Pet_policy_supports_every_governed_status(PetAccessStatus status)
    {
        var policy = new PetPolicy(status, [PetSpecies.Dog], [new("leash-required")], "", "");
        Assert.Equal(status, policy.Status);
        Assert.Single(policy.Conditions);
    }

    [Fact]
    public void Verified_manual_assertion_precedes_newer_automatic_assertion()
    {
        var now = DateTimeOffset.UtcNow;
        var placeId = Guid.NewGuid();
        var manual = Assertion(placeId, PetAccessStatus.Allowed, PetAssertionOrigin.Manual, now.AddDays(-2), now.AddDays(-2));
        var automatic = Assertion(placeId, PetAccessStatus.NotAllowed, PetAssertionOrigin.Automatic, now, null);
        Assert.Same(manual, new PetAssertionPrecedencePolicy().SelectEffective([automatic, manual], now));
    }

    [Fact]
    public void Revoked_or_expired_manual_assertion_does_not_precede_current_automatic_data()
    {
        var now = DateTimeOffset.UtcNow;
        var placeId = Guid.NewGuid();
        var revoked = Assertion(placeId, PetAccessStatus.Allowed, PetAssertionOrigin.Manual, now.AddDays(-2), now.AddDays(-2), PetAssertionState.Revoked);
        var automatic = Assertion(placeId, PetAccessStatus.PendingReview, PetAssertionOrigin.Automatic, now, null);
        Assert.Same(automatic, new PetAssertionPrecedencePolicy().SelectEffective([revoked, automatic], now));
    }

    [Fact]
    public void Local_verified_policy_precedes_chain_and_chain_requires_applicability_evidence()
    {
        var chainPolicy = new ChainPetPolicy(Guid.NewGuid(), Guid.NewGuid(),
            new PetPolicy(PetAccessStatus.Allowed, [PetSpecies.Dog], [], "", ""), new("ES"),
            DateTimeOffset.UtcNow.AddDays(-1), null, false, "chain-web", DateTimeOffset.UtcNow);
        var resolver = new ChainPetPolicyResolver();
        Assert.Equal(PetAccessStatus.Unknown, resolver.Resolve(null, chainPolicy, DateTimeOffset.UtcNow).Status);

        var local = new PetPolicy(PetAccessStatus.NotAllowed, [PetSpecies.Dog], [], "", "");
        Assert.Same(local, resolver.Resolve(local, chainPolicy, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Validator_allows_null_coordinate_pair_but_rejects_a_half_pair()
    {
        var valid = Request(null, null);
        var validator = new Zuppeto.Application.Places.Validators.PlaceUpsertRequestValidator();
        Assert.True(validator.Validate(valid).IsValid);
        Assert.False(validator.Validate(Request(41m, null)).IsValid);
    }

    [Fact]
    public void Persistence_round_trip_preserves_extensible_species_conditions_and_null_coordinates()
    {
        var place = new Place(Guid.NewGuid(), "Lloc", PlaceType.Restaurant, "", "", "",
            new PostalAddress("Carrer", "Barcelona", "Espanya", ""), null,
            new PetPolicy(PetAccessStatus.TerraceOnly, [PetSpecies.Dog, new PetSpecies("rabbit")],
                [new PetAccessCondition("maximum-animals", "2")], "Només terrassa", ""),
            new Pricing(""), new RatingSnapshot(0, 0));

        var restored = PlacePersistenceMapper.ToDomain(PlacePersistenceMapper.ToRecord(place));

        Assert.Null(restored.Location);
        Assert.Equal(PetAccessStatus.TerraceOnly, restored.PetPolicy.Status);
        Assert.Contains(restored.PetPolicy.Species, x => x.Code == "rabbit");
        Assert.Contains(restored.PetPolicy.Conditions, x => x.Code == "maximum-animals" && x.Value == "2");
    }

    private static PetFriendlyAssertion Assertion(Guid placeId, PetAccessStatus status, PetAssertionOrigin origin,
        DateTimeOffset observed, DateTimeOffset? verified, PetAssertionState state = PetAssertionState.Current) =>
        new(Guid.NewGuid(), placeId, PetSpecies.Dog, status, [], "test", null, status.ToString(), observed,
            origin, PetConfidence.Confirmed, PetVerificationMethod.OnSite, verified, verified is null ? null : Guid.NewGuid(), state: state);

    private static PlaceUpsertRequest Request(decimal? latitude, decimal? longitude) => new(
        null, "Lloc", "Restaurant", "", "", "", "Carrer", "Barcelona", "Espanya", "",
        latitude, longitude, false, false, "", "", "", 0, 0, [], [], PetAccessStatus: "Unknown");
}
