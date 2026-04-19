using YepPet.Domain.Permissions;

namespace YepPet.Domain.Abstractions;

public interface IRolePermissionRepository
{
    Task<IReadOnlyCollection<PermissionDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RolePermissionAssignment>> GetAssignmentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetPermissionKeysByRoleAsync(
        string roleKey,
        CancellationToken cancellationToken = default);

    Task ReplaceRolePermissionsAsync(
        string roleKey,
        IReadOnlyCollection<string> permissionKeys,
        CancellationToken cancellationToken = default);

    Task<bool> PermissionKeyExistsAsync(string key, CancellationToken cancellationToken = default);

    Task<PermissionDefinition> AddPermissionDefinitionAsync(
        PermissionDefinition definition,
        CancellationToken cancellationToken = default);

    Task<PermissionDefinition?> UpdatePermissionDefinitionAsync(
        string key,
        string scopeType,
        string displayName,
        string description,
        string? scopePayload,
        CancellationToken cancellationToken = default);

    Task<bool> DeletePermissionDefinitionAsync(string key, CancellationToken cancellationToken = default);
}
