namespace YepPet.Application.Admin;

public interface IAdminApplicationService
{
    Task<IReadOnlyCollection<AdminUserListItemDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<Users.UserDto> CreateUserAsync(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken = default);

    Task<Users.UserDto?> UpdateUserRoleAsync(
        Guid userId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<RolePermissionCatalogDto> GetRolePermissionsAsync(CancellationToken cancellationToken = default);

    Task<RolePermissionCatalogDto> UpdateRolePermissionsAsync(
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminMenuCatalogDto> GetMenusAsync(CancellationToken cancellationToken = default);

    Task<AdminMenuCatalogDto> SaveMenuAsync(
        SaveMenuRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetPermissionKeysByRoleAsync(
        string role,
        CancellationToken cancellationToken = default);

    IReadOnlyCollection<InternalDocumentSummaryDto> GetInternalDocumentIndex();

    Task<InternalDocumentDto?> GetInternalDocumentAsync(string key, CancellationToken cancellationToken = default);
}
