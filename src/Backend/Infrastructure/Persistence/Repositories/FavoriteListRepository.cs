using Microsoft.EntityFrameworkCore;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Favorites;
using Zuppeto.Infrastructure.Persistence.Entities;
using Zuppeto.Infrastructure.Persistence.Mappings;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

internal sealed class FavoriteListRepository(ZuppetoDbContext dbContext) : IFavoriteListRepository
{
    public async Task<FavoriteList?> GetByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.FavoriteLists
            .AsNoTracking()
            .Include(favoriteList => favoriteList.Entries)
            .FirstOrDefaultAsync(favoriteList => favoriteList.OwnerUserId == ownerUserId, cancellationToken);

        return record is null ? null : FavoriteListPersistenceMapper.ToDomain(record);
    }

    public Task<bool> ExistsByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return dbContext.FavoriteLists.AnyAsync(favoriteList => favoriteList.OwnerUserId == ownerUserId, cancellationToken);
    }

    public async Task AddAsync(FavoriteList favoriteList, CancellationToken cancellationToken = default)
    {
        var record = FavoriteListPersistenceMapper.ToRecord(favoriteList);

        await dbContext.FavoriteLists.AddAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FavoriteList favoriteList, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.FavoriteLists
            .Include(current => current.Entries)
            .FirstOrDefaultAsync(current => current.Id == favoriteList.Id, cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException("No s’ha trobat la llista de preferits.");
        }

        FavoriteListPersistenceMapper.Apply(favoriteList, record);

        var desiredById = favoriteList.Entries.ToDictionary(entry => entry.Id);
        var existingById = record.Entries.ToDictionary(entry => entry.Id);

        foreach (var stale in existingById.Values.Where(entry => !desiredById.ContainsKey(entry.Id)).ToArray())
        {
            record.Entries.Remove(stale);
            dbContext.FavoriteEntries.Remove(stale);
        }

        foreach (var entry in favoriteList.Entries)
        {
            if (existingById.TryGetValue(entry.Id, out var current))
            {
                current.PlaceId = entry.PlaceId;
                current.SavedAtUtc = entry.SavedAtUtc;
                continue;
            }

            var created = new FavoriteEntryRecord
            {
                Id = entry.Id,
                FavoriteListId = favoriteList.Id,
                PlaceId = entry.PlaceId,
                SavedAtUtc = entry.SavedAtUtc
            };

            record.Entries.Add(created);
            await dbContext.FavoriteEntries.AddAsync(created, cancellationToken);
            // Client-assigned GUID must not stay temporary or EF emits UPDATE instead of INSERT.
            var tracked = dbContext.Entry(created);
            tracked.State = EntityState.Added;
            tracked.Property(e => e.Id).IsTemporary = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
