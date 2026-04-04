using YepPet.Domain.Users;

namespace YepPet.Application.Navigation;

public interface INavigationApplicationService
{
    Task<IReadOnlyCollection<NavigationMenuItemDto>> GetMenuForRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default);
}
