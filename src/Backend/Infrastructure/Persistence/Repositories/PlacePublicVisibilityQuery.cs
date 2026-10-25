using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

internal sealed class PlacePublicVisibilityQuery(ZuppetoDbContext dbContext) : IPlacePublicVisibilityQuery
{
    public Task<bool> IsVisibleAsync(Guid placeId, CancellationToken cancellationToken = default) =>
        PubliclyVisiblePlaces()
            .AnyAsync(place => place.Id == placeId, cancellationToken);

    public async Task<IReadOnlySet<Guid>> FilterVisibleAsync(
        IReadOnlyCollection<Guid> placeIds,
        CancellationToken cancellationToken = default)
    {
        if (placeIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var ids = await PubliclyVisiblePlaces()
            .Where(place => placeIds.Contains(place.Id))
            .Select(place => place.Id)
            .ToArrayAsync(cancellationToken);

        return ids.ToHashSet();
    }

    private IQueryable<Entities.PlaceRecord> PubliclyVisiblePlaces() =>
        dbContext.Places.AsNoTracking().ApplyPublicVisibility(dbContext);
}
