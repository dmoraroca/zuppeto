using YepPet.Domain.Navigation;

namespace YepPet.Domain.Abstractions;

public interface IMenuRepository
{
    Task<IReadOnlyCollection<MenuItemDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MenuRoleAssignment>> GetAssignmentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MenuItemDefinition>> GetMenuItemsByRoleAsync(
        string roleKey,
        CancellationToken cancellationToken = default);

    Task SaveDefinitionAsync(
        MenuItemDefinition definition,
        CancellationToken cancellationToken = default);

    Task ReplaceMenuRolesAsync(
        string menuKey,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default);
}
