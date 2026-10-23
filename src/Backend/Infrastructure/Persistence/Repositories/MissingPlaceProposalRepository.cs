using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Places;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

internal sealed class MissingPlaceProposalRepository(ZuppetoDbContext db) : IMissingPlaceProposalRepository
{
    public Task<int> CountRecentByUserAsync(Guid userId, DateTimeOffset sinceUtc, CancellationToken cancellationToken = default) =>
        db.MissingPlaceProposals.AsNoTracking().CountAsync(
            item => item.SubmittedByUserId == userId && item.CreatedAtUtc >= sinceUtc, cancellationToken);

    public Task<Guid?> FindPossibleDuplicateAsync(string name, string municipality, CancellationToken cancellationToken = default) =>
        db.Places.AsNoTracking().Where(place =>
            EF.Functions.ILike(place.Name, name) && EF.Functions.ILike(place.City, municipality))
            .Select(place => (Guid?)place.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid> CreateAsync(Guid userId, MissingPlaceProposalRequest request, Guid? possibleDuplicatePlaceId,
        CancellationToken cancellationToken = default)
    {
        var record = new MissingPlaceProposalRecord
        {
            Id = Guid.NewGuid(), SubmittedByUserId = userId, Name = request.Name,
            Municipality = request.Municipality, ApproximateAddress = request.ApproximateAddress,
            AdditionalInformation = request.AdditionalInformation, PossibleDuplicatePlaceId = possibleDuplicatePlaceId,
            Status = "Pending", CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.MissingPlaceProposals.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        return record.Id;
    }

    public async Task<IReadOnlyCollection<MissingPlaceProposalDto>> ListPendingAsync(int skip, int take,
        CancellationToken cancellationToken = default) =>
        await db.MissingPlaceProposals.AsNoTracking().Where(item => item.Status == "Pending")
            .OrderBy(item => item.CreatedAtUtc).Skip(skip).Take(take)
            .Select(item => new MissingPlaceProposalDto(item.Id, item.SubmittedByUserId, item.Name,
                item.Municipality, item.ApproximateAddress, item.AdditionalInformation,
                item.PossibleDuplicatePlaceId, item.Status, item.CreatedAtUtc)).ToArrayAsync(cancellationToken);

    public async Task DecideAsync(Guid proposalId, Guid actorId, string decision, string reason,
        CancellationToken cancellationToken = default)
    {
        var proposal = await db.MissingPlaceProposals.SingleOrDefaultAsync(item => item.Id == proposalId, cancellationToken)
            ?? throw new KeyNotFoundException("No s’ha trobat la proposta.");
        if (proposal.Status != "Pending") throw new InvalidOperationException("La proposta ja està resolta.");
        proposal.Status = decision;
        proposal.ReviewedByUserId = actorId;
        proposal.ReviewReason = reason;
        proposal.ReviewedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
