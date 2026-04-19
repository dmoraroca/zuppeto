using Microsoft.EntityFrameworkCore;
using YepPet.Domain.Abstractions;
using YepPet.Domain.Roles;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Repositories;

internal sealed class RoleCatalogRepository(YepPetDbContext dbContext) : IRoleCatalogRepository
{
    public async Task<IReadOnlyList<RoleCatalogRow>> ListAsync(CancellationToken cancellationToken = default)
    {
        var records = await dbContext.Roles
            .AsNoTracking()
            .OrderBy(r => r.Key)
            .ToListAsync(cancellationToken);

        return records.Select(ToRow).ToArray();
    }

    public async Task<RoleCatalogRow?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalized = key.Trim();
        var record = await dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == normalized, cancellationToken);

        return record is null ? null : ToRow(record);
    }

    public Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalized = key.Trim();
        return dbContext.Roles.AsNoTracking().AnyAsync(r => r.Key == normalized, cancellationToken);
    }

    public async Task<RoleCatalogRow> CreateAsync(string key, string displayName, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new RoleRecord
        {
            Id = Guid.NewGuid(),
            Key = key.Trim(),
            DisplayName = displayName.Trim(),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await dbContext.Roles.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToRow(entity);
    }

    public async Task<RoleCatalogRow?> UpdateAsync(
        string key,
        string displayName,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var normalized = key.Trim();
        var entity = await dbContext.Roles.FirstOrDefaultAsync(r => r.Key == normalized, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.DisplayName = displayName.Trim();
        entity.IsActive = isActive;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToRow(entity);
    }

    public async Task<RoleDeleteOutcome> TryDeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalized = key.Trim();
        var entity = await dbContext.Roles.FirstOrDefaultAsync(r => r.Key == normalized, cancellationToken);
        if (entity is null)
        {
            return RoleDeleteOutcome.NotFound;
        }

        var userCount = await dbContext.Users.CountAsync(u => u.Role == normalized, cancellationToken);
        if (userCount > 0)
        {
            return RoleDeleteOutcome.HasDependencies;
        }

        var permissionLinks = await dbContext.RolePermissions.CountAsync(rp => rp.Role == normalized, cancellationToken);
        if (permissionLinks > 0)
        {
            return RoleDeleteOutcome.HasDependencies;
        }

        var menuLinks = await dbContext.MenuRoles.CountAsync(mr => mr.Role == normalized, cancellationToken);
        if (menuLinks > 0)
        {
            return RoleDeleteOutcome.HasDependencies;
        }

        dbContext.Roles.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return RoleDeleteOutcome.Deleted;
    }

    private static RoleCatalogRow ToRow(RoleRecord record) =>
        new(
            record.Id,
            record.Key,
            record.DisplayName,
            record.IsActive,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);
}
