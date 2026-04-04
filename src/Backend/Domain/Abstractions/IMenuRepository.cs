using YepPet.Domain.Navigation;
using YepPet.Domain.Users;

namespace YepPet.Domain.Abstractions;

public interface IMenuRepository
{
    Task<IReadOnlyCollection<MenuItemDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MenuRoleAssignment>> GetAssignmentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MenuItemDefinition>> GetMenuItemsByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default);

    Task SaveDefinitionAsync(
        MenuItemDefinition definition,
        CancellationToken cancellationToken = default);

    Task ReplaceMenuRolesAsync(
        string menuKey,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default);
}
