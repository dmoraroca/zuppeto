using Microsoft.EntityFrameworkCore;
using YepPet.Domain.Abstractions;
using YepPet.Domain.Permissions;
using YepPet.Domain.Users;
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
                permission.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RolePermissionAssignment>> GetAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.RolePermissions
            .AsNoTracking()
            .OrderBy(record => record.Role)
            .ThenBy(record => record.PermissionKey)
            .Select(record => new RolePermissionAssignment(
                Enum.Parse<UserRole>(record.Role, ignoreCase: true),
                record.PermissionKey))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionKeysByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var roleValue = role.ToString();

        return await dbContext.RolePermissions
            .AsNoTracking()
            .Where(record => record.Role == roleValue)
            .OrderBy(record => record.PermissionKey)
            .Select(record => record.PermissionKey)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceRolePermissionsAsync(
        UserRole role,
        IReadOnlyCollection<string> permissionKeys,
        CancellationToken cancellationToken = default)
    {
        var roleValue = role.ToString();
        var existing = await dbContext.RolePermissions
            .Where(record => record.Role == roleValue)
            .ToListAsync(cancellationToken);

        dbContext.RolePermissions.RemoveRange(existing);

        var normalizedKeys = permissionKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedKeys.Length > 0)
        {
            var validKeys = await dbContext.Permissions
                .Where(permission => normalizedKeys.Contains(permission.Key))
                .Select(permission => permission.Key)
                .ToListAsync(cancellationToken);

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

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
