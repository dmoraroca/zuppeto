using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Navigation;

namespace Zuppeto.Application.Navigation;

internal sealed class NavigationApplicationService(IMenuRepository menuRepository) : INavigationApplicationService
{
    public async Task<IReadOnlyCollection<NavigationMenuItemDto>> GetMenuForRoleAsync(
        string roleKey,
        CancellationToken cancellationToken = default)
    {
        var items = await menuRepository.GetMenuItemsByRoleAsync(roleKey, cancellationToken);
        if (IsHomeOnlyRole(roleKey))
        {
            var catalog = await menuRepository.GetDefinitionsAsync(cancellationToken);
            items = AttachHomeAndHelpRoots(items, catalog);
        }
        var itemsByParent = items
            .GroupBy(item => item.ParentKey ?? string.Empty)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.SortOrder).ToArray(), StringComparer.Ordinal);

        IReadOnlyCollection<NavigationMenuItemDto> Build(string? parentKey)
        {
            var lookupKey = parentKey ?? string.Empty;

            if (!itemsByParent.TryGetValue(lookupKey, out var children))
            {
                return [];
            }

            return children
                .Select(item =>
                {
                    var label = item.Label;

                    if (item.Key.Equals("admin", StringComparison.Ordinal))
                    {
                        label = string.Equals(roleKey, "Developer", StringComparison.OrdinalIgnoreCase)
                            ? "Del desenvolupador"
                            : "Del administrador";
                    }

                    return new NavigationMenuItemDto(
                        item.Key,
                        label,
                        item.Route,
                        Build(item.Key));
                })
                .ToArray();
        }

        return Build(parentKey: null);
    }

    private static bool IsHomeOnlyRole(string roleKey)
    {
        var key = roleKey.Trim();
        return !key.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            && !key.Equals("User", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Home-only roles always get Inici and Ajuda from the catalog, plus every menu assigned to the role.
    /// </summary>
    private static IReadOnlyCollection<MenuItemDefinition> AttachHomeAndHelpRoots(
        IReadOnlyCollection<MenuItemDefinition> items,
        IReadOnlyCollection<MenuItemDefinition> catalog)
    {
        var merged = items.ToList();
        foreach (var root in catalog.Where(item =>
                     item.IsActive
                     && (item.Key.Equals("home", StringComparison.OrdinalIgnoreCase)
                         || item.Key.Equals("help", StringComparison.OrdinalIgnoreCase))))
        {
            if (!merged.Any(item => item.Key.Equals(root.Key, StringComparison.OrdinalIgnoreCase)))
            {
                merged.Add(root);
            }
        }

        return merged;
    }
}
