using Microsoft.EntityFrameworkCore;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Reviews;
using Zuppeto.Infrastructure.Persistence.Mappings;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

internal sealed class PlaceReviewRepository(ZuppetoDbContext dbContext) : IPlaceReviewRepository
{
    public async Task<PlaceReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.PlaceReviews
            .AsNoTracking()
            .FirstOrDefaultAsync(review => review.Id == id, cancellationToken);

        return record is null ? null : PlaceReviewPersistenceMapper.ToDomain(record);
    }

    public async Task<PlaceReview?> GetByAuthorAndPlaceAsync(
        Guid authorUserId,
        Guid placeId,
        CancellationToken cancellationToken = default)
    {
        var record = await dbContext.PlaceReviews
            .AsNoTracking()
            .FirstOrDefaultAsync(
                review => review.AuthorUserId == authorUserId && review.PlaceId == placeId,
                cancellationToken);

        return record is null ? null : PlaceReviewPersistenceMapper.ToDomain(record);
    }

    public async Task<IReadOnlyCollection<PlaceReview>> GetByPlaceAsync(
        Guid placeId,
        PlaceReviewQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Entities.PlaceReviewRecord> reviewQuery = dbContext.PlaceReviews
            .AsNoTracking()
            .Where(review => review.PlaceId == placeId);

        if (query.OnlyVisible)
        {
            reviewQuery = reviewQuery.Where(review => review.IsVisible);
        }

        var records = await reviewQuery
            .OrderByDescending(review => review.CreatedAtUtc)
            .Take(query.Take)
            .ToArrayAsync(cancellationToken);

        return records
            .Select(PlaceReviewPersistenceMapper.ToDomain)
            .ToArray();
    }

    public async Task AddAsync(PlaceReview review, CancellationToken cancellationToken = default)
    {
        var record = PlaceReviewPersistenceMapper.ToRecord(review);

        await dbContext.PlaceReviews.AddAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PlaceReview review, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.PlaceReviews
            .FirstOrDefaultAsync(current => current.Id == review.Id, cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException("No s’ha trobat la ressenya.");
        }

        PlaceReviewPersistenceMapper.Apply(review, record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
