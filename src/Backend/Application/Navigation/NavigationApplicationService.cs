using YepPet.Domain.Abstractions;

namespace YepPet.Application.Navigation;

internal sealed class NavigationApplicationService(IMenuRepository menuRepository) : INavigationApplicationService
{
    public async Task<IReadOnlyCollection<NavigationMenuItemDto>> GetMenuForRoleAsync(
        string roleKey,
        CancellationToken cancellationToken = default)
    {
        var items = await menuRepository.GetMenuItemsByRoleAsync(roleKey, cancellationToken);
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
}
