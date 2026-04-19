namespace YepPet.Application.Navigation;

public interface INavigationApplicationService
{
    Task<IReadOnlyCollection<NavigationMenuItemDto>> GetMenuForRoleAsync(
        string roleKey,
        CancellationToken cancellationToken = default);
}
