using Microsoft.EntityFrameworkCore;
using YepPet.Domain.Abstractions;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Repositories;

internal sealed class PlaceSearchQueryRepository(YepPetDbContext dbContext) : IPlaceSearchQueryRepository
{
    public async Task<IReadOnlyList<Guid>?> TryGetFreshPlaceIdsAsync(
        IPlaceSearchQueryRepository.SearchSnapshotKey key,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        var queryKey = BuildQueryKey(key);
        var query = await dbContext.PlaceSearchQueries
            .Include(row => row.Results)
            .FirstOrDefaultAsync(row => row.QueryKey == queryKey, cancellationToken);

        if (query is null || query.ExpiresAtUtc <= nowUtc)
        {
            return null;
        }

        query.HitCount += 1;
        query.LastRunAtUtc = nowUtc;
        query.UpdatedAtUtc = nowUtc;
        await dbContext.SaveChangesAsync(cancellationToken);

        return query.Results
            .OrderBy(row => row.Rank)
            .Select(row => row.PlaceId)
            .ToArray();
    }

    public async Task SaveSnapshotAsync(
        IPlaceSearchQueryRepository.SearchSnapshotKey key,
        IReadOnlyList<Guid> orderedPlaceIds,
        DateTimeOffset nowUtc,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var queryKey = BuildQueryKey(key);
        var row = await dbContext.PlaceSearchQueries
            .Include(item => item.Results)
            .FirstOrDefaultAsync(item => item.QueryKey == queryKey, cancellationToken);

        if (row is null)
        {
            row = new PlaceSearchQueryRecord
            {
                Id = Guid.NewGuid(),
                QueryKey = queryKey,
                SearchText = key.SearchText.Trim(),
                City = key.City.Trim(),
                Type = key.Type.Trim(),
                PetCategory = key.PetCategory.Trim(),
                HitCount = 1,
                CreatedAtUtc = nowUtc
            };
            await dbContext.PlaceSearchQueries.AddAsync(row, cancellationToken);
        }
        else
        {
            row.SearchText = key.SearchText.Trim();
            row.City = key.City.Trim();
            row.Type = key.Type.Trim();
            row.PetCategory = key.PetCategory.Trim();
            row.HitCount += 1;
            dbContext.PlaceSearchQueryResults.RemoveRange(row.Results);
            row.Results.Clear();
        }

        row.ResultCount = orderedPlaceIds.Count;
        row.LastRunAtUtc = nowUtc;
        row.ExpiresAtUtc = nowUtc.Add(ttl);
        row.UpdatedAtUtc = nowUtc;

        for (var i = 0; i < orderedPlaceIds.Count; i++)
        {
            row.Results.Add(new PlaceSearchQueryResultRecord
            {
                QueryId = row.Id,
                PlaceId = orderedPlaceIds[i],
                Rank = i,
                CapturedAtUtc = nowUtc
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<IPlaceSearchQueryRepository.PlaceSearchHistoryRow>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var clamped = Math.Clamp(limit, 1, 100);
        return await dbContext.PlaceSearchQueries
            .AsNoTracking()
            .OrderByDescending(item => item.LastRunAtUtc)
            .Take(clamped)
            .Select(item => new IPlaceSearchQueryRepository.PlaceSearchHistoryRow(
                item.SearchText,
                item.City,
                item.Type,
                item.PetCategory,
                item.HitCount,
                item.ResultCount,
                item.LastRunAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    private static string BuildQueryKey(IPlaceSearchQueryRepository.SearchSnapshotKey key)
    {
        var search = key.SearchText.Trim().ToLowerInvariant();
        var city = key.City.Trim().ToLowerInvariant();
        var type = key.Type.Trim().ToLowerInvariant();
        var pet = key.PetCategory.Trim().ToLowerInvariant();
        return $"{search}|{city}|{type}|{pet}";
    }
}
