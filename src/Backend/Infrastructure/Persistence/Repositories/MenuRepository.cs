using Microsoft.EntityFrameworkCore;
using YepPet.Domain.Abstractions;
using YepPet.Domain.Navigation;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Repositories;

internal sealed class MenuRepository(YepPetDbContext dbContext) : IMenuRepository
{
    public async Task<IReadOnlyCollection<MenuItemDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Menus
            .AsNoTracking()
            .OrderBy(menu => menu.SortOrder)
            .ThenBy(menu => menu.Key)
            .Select(menu => new MenuItemDefinition(
                menu.Key,
                menu.Label,
                menu.Route,
                menu.ParentKey,
                menu.SortOrder,
                menu.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MenuRoleAssignment>> GetAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.MenuRoles
            .AsNoTracking()
            .OrderBy(menuRole => menuRole.MenuKey)
            .ThenBy(menuRole => menuRole.Role)
            .Select(menuRole => new MenuRoleAssignment(menuRole.MenuKey, menuRole.Role))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MenuItemDefinition>> GetMenuItemsByRoleAsync(
        string roleKey,
        CancellationToken cancellationToken = default)
    {
        var roleValue = roleKey.Trim();

        return await dbContext.MenuRoles
            .AsNoTracking()
            .Where(menuRole => menuRole.Role == roleValue && menuRole.Menu != null && menuRole.Menu.IsActive)
            .OrderBy(menuRole => menuRole.Menu!.SortOrder)
            .Select(menuRole => new MenuItemDefinition(
                menuRole.Menu!.Key,
                menuRole.Menu.Label,
                menuRole.Menu.Route,
                menuRole.Menu.ParentKey,
                menuRole.Menu.SortOrder,
                menuRole.Menu.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task SaveDefinitionAsync(
        MenuItemDefinition definition,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Menus
            .FirstOrDefaultAsync(menu => menu.Key == definition.Key, cancellationToken);

        if (existing is null)
        {
            dbContext.Menus.Add(new MenuRecord
            {
                Id = Guid.NewGuid(),
                Key = definition.Key,
                Label = definition.Label,
                Route = definition.Route,
                ParentKey = definition.ParentKey,
                SortOrder = definition.SortOrder,
                IsActive = definition.IsActive
            });
        }
        else
        {
            existing.Label = definition.Label;
            existing.Route = definition.Route;
            existing.ParentKey = definition.ParentKey;
            existing.SortOrder = definition.SortOrder;
            existing.IsActive = definition.IsActive;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceMenuRolesAsync(
        string menuKey,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.MenuRoles
            .Where(menuRole => menuRole.MenuKey == menuKey)
            .ToListAsync(cancellationToken);

        dbContext.MenuRoles.RemoveRange(existing);

        var normalizedRoles = roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var role in normalizedRoles)
        {
            dbContext.MenuRoles.Add(new MenuRoleRecord
            {
                Id = Guid.NewGuid(),
                MenuKey = menuKey,
                Role = role
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
