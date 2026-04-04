using YepPet.Domain.Abstractions;
using YepPet.Domain.Users;

namespace YepPet.Application.Navigation;

internal sealed class NavigationApplicationService(IMenuRepository menuRepository) : INavigationApplicationService
{
    public async Task<IReadOnlyCollection<NavigationMenuItemDto>> GetMenuForRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var items = await menuRepository.GetMenuItemsByRoleAsync(role, cancellationToken);
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
                .Select(item => new NavigationMenuItemDto(
                    item.Key,
                    item.Label,
                    item.Route,
                    Build(item.Key)))
                .ToArray();
        }

        return Build(parentKey: null);
    }
}
