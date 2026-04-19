using Microsoft.EntityFrameworkCore;
using YepPet.Domain.Abstractions;
using YepPet.Domain.Permissions;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Repositories;

internal sealed class RolePermissionRepository(YepPetDbContext dbContext) : IRolePermissionRepository
{
    public async Task<IReadOnlyCollection<PermissionDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Permissions
            .AsNoTracking()
            .OrderBy(permission => permission.ScopeType)
            .ThenBy(permission => permission.Key)
            .Select(permission => new PermissionDefinition(
                permission.Key,
                permission.ScopeType,
                permission.DisplayName,
                permission.Description,
                permission.ScopePayload,
                permission.CreatedAtUtc,
                permission.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RolePermissionAssignment>> GetAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.RolePermissions
            .AsNoTracking()
            .OrderBy(record => record.Role)
            .ThenBy(record => record.PermissionKey)
            .Select(record => new RolePermissionAssignment(
                record.Role,
                record.PermissionKey))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionKeysByRoleAsync(
        string roleKey,
        CancellationToken cancellationToken = default)
    {
        var roleValue = roleKey.Trim();

        return await dbContext.RolePermissions
            .AsNoTracking()
            .Where(record => record.Role == roleValue)
            .OrderBy(record => record.PermissionKey)
            .Select(record => record.PermissionKey)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceRolePermissionsAsync(
        string roleKey,
        IReadOnlyCollection<string> permissionKeys,
        CancellationToken cancellationToken = default)
    {
        var roleValue = roleKey.Trim();
        var existing = await dbContext.RolePermissions
            .Where(record => record.Role == roleValue)
            .ToListAsync(cancellationToken);

        var oldKeys = existing
            .Select(record => record.PermissionKey)
            .ToHashSet(StringComparer.Ordinal);

        dbContext.RolePermissions.RemoveRange(existing);

        var normalizedKeys = permissionKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        HashSet<string> newKeysSet;
        if (normalizedKeys.Length == 0)
        {
            newKeysSet = new HashSet<string>(StringComparer.Ordinal);
        }
        else
        {
            var validKeys = await dbContext.Permissions
                .Where(permission => normalizedKeys.Contains(permission.Key))
                .Select(permission => permission.Key)
                .ToListAsync(cancellationToken);

            newKeysSet = validKeys.ToHashSet(StringComparer.Ordinal);

            foreach (var permissionKey in validKeys)
            {
                dbContext.RolePermissions.Add(new RolePermissionRecord
                {
                    Id = Guid.NewGuid(),
                    Role = roleValue,
                    PermissionKey = permissionKey
                });
            }
        }

        var touchedKeys = oldKeys
            .Except(newKeysSet)
            .Concat(newKeysSet.Except(oldKeys))
            .ToArray();

        if (touchedKeys.Length > 0)
        {
            var now = DateTimeOffset.UtcNow;
            var permissions = await dbContext.Permissions
                .Where(permission => touchedKeys.Contains(permission.Key))
                .ToListAsync(cancellationToken);

            foreach (var permission in permissions)
            {
                permission.UpdatedAtUtc = now;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> PermissionKeyExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await dbContext.Permissions.AnyAsync(permission => permission.Key == key, cancellationToken);
    }

    public async Task<PermissionDefinition> AddPermissionDefinitionAsync(
        PermissionDefinition definition,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new PermissionRecord
        {
            Id = Guid.NewGuid(),
            Key = definition.Key,
            ScopeType = definition.ScopeType,
            DisplayName = definition.DisplayName,
            Description = definition.Description,
            ScopePayload = definition.ScopePayload,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Permissions.Add(entity);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PermissionDefinition(
            entity.Key,
            entity.ScopeType,
            entity.DisplayName,
            entity.Description,
            entity.ScopePayload,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
    }

    public async Task<PermissionDefinition?> UpdatePermissionDefinitionAsync(
        string key,
        string scopeType,
        string displayName,
        string description,
        string? scopePayload,
        CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Permissions.FirstOrDefaultAsync(permission => permission.Key == key, cancellationToken);
        if (record is null)
        {
            return null;
        }

        record.ScopeType = scopeType;
        record.DisplayName = displayName;
        record.Description = description;
        record.ScopePayload = scopePayload;
        record.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PermissionDefinition(
            record.Key,
            record.ScopeType,
            record.DisplayName,
            record.Description,
            record.ScopePayload,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);
    }

    public async Task<bool> DeletePermissionDefinitionAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalized = key.Trim();
        var assignments = await dbContext.RolePermissions
            .Where(record => record.PermissionKey == normalized)
            .ToListAsync(cancellationToken);

        if (assignments.Count > 0)
        {
            dbContext.RolePermissions.RemoveRange(assignments);
        }

        var permission = await dbContext.Permissions.FirstOrDefaultAsync(p => p.Key == normalized, cancellationToken);
        if (permission is null)
        {
            return false;
        }

        dbContext.Permissions.Remove(permission);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
