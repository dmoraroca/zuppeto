using Zuppeto.Application.Places;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class MissingPlaceProposalTests
{
    [Fact]
    public async Task Proposal_is_pending_and_preserves_a_possible_duplicate_for_admin_review()
    {
        var duplicate = Guid.NewGuid();
        var repository = new FakeRepository { Duplicate = duplicate };
        var result = await new MissingPlaceProposalService(repository).ProposeAsync(Guid.NewGuid(),
            new MissingPlaceProposalRequest(" Cafè Mar ", " Arenys de Mar ", " Riera 1 ", " Té terrassa "));

        Assert.Equal("Pending", result.Status);
        Assert.Equal(duplicate, result.PossibleDuplicatePlaceId);
        Assert.Equal("Cafè Mar", repository.Saved!.Name);
    }

    [Fact]
    public async Task Proposal_rate_limit_blocks_abusive_submissions()
    {
        var repository = new FakeRepository { RecentCount = 5 };
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MissingPlaceProposalService(repository).ProposeAsync(Guid.NewGuid(),
                new MissingPlaceProposalRequest("Cafè Mar", "Arenys", "Riera 1", null)));
        Assert.Contains("límit", error.Message);
        Assert.Null(repository.Saved);
    }

    private sealed class FakeRepository : IMissingPlaceProposalRepository
    {
        public int RecentCount { get; init; }
        public Guid? Duplicate { get; init; }
        public MissingPlaceProposalRequest? Saved { get; private set; }
        public Task<int> CountRecentByUserAsync(Guid userId, DateTimeOffset sinceUtc, CancellationToken cancellationToken = default) => Task.FromResult(RecentCount);
        public Task<Guid?> FindPossibleDuplicateAsync(string name, string municipality, CancellationToken cancellationToken = default) => Task.FromResult(Duplicate);
        public Task<Guid> CreateAsync(Guid userId, MissingPlaceProposalRequest request, Guid? possibleDuplicatePlaceId, CancellationToken cancellationToken = default)
        { Saved = request; return Task.FromResult(Guid.NewGuid()); }
        public Task<IReadOnlyCollection<MissingPlaceProposalDto>> ListPendingAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<MissingPlaceProposalDto>>([]);
        public Task DecideAsync(Guid proposalId, Guid actorId, string decision, string reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
