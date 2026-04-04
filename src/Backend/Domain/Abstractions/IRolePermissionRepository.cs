using YepPet.Domain.Permissions;
using YepPet.Domain.Users;

namespace YepPet.Domain.Abstractions;

public interface IRolePermissionRepository
{
    Task<IReadOnlyCollection<PermissionDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RolePermissionAssignment>> GetAssignmentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetPermissionKeysByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default);

    Task ReplaceRolePermissionsAsync(
        UserRole role,
        IReadOnlyCollection<string> permissionKeys,
        CancellationToken cancellationToken = default);
}
