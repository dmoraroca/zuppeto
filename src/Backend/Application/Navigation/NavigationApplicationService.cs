using Zuppeto.Domain.Abstractions;

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
            items = items
                .Where(item =>
                    item.Key.Equals("home", StringComparison.OrdinalIgnoreCase)
                    || item.Key.Equals("help", StringComparison.OrdinalIgnoreCase)
                    || item.Key.StartsWith("help.", StringComparison.OrdinalIgnoreCase))
                .ToArray();
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
}
