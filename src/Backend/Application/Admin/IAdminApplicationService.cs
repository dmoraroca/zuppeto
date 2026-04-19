using YepPet.Application.Results;

namespace YepPet.Application.Admin;

public interface IAdminApplicationService
{
    Task<IReadOnlyCollection<AdminUserListItemDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<Results.Result<Users.UserDto>> CreateUserAsync(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken = default);

    Task<Results.Result<Users.UserDto>> UpdateUserRoleAsync(
        Guid userId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<RolePermissionCatalogDto> GetRolePermissionsAsync(CancellationToken cancellationToken = default);

    Task<Result<RolePermissionCatalogDto>> UpdateRolePermissionsAsync(
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PermissionDefinitionDto>> CreatePermissionDefinitionAsync(
        CreatePermissionDefinitionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PermissionDefinitionDto>> UpdatePermissionDefinitionAsync(
        string key,
        UpdatePermissionDefinitionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeletePermissionDefinitionAsync(string key, CancellationToken cancellationToken = default);

    Task<AdminMenuCatalogDto> GetMenusAsync(CancellationToken cancellationToken = default);

    Task<AdminMenuCatalogDto> SaveMenuAsync(
        SaveMenuRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetPermissionKeysByRoleAsync(
        string role,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RoleDefinitionDto>> GetRoleDefinitionsAsync(CancellationToken cancellationToken = default);

    Task<Result<RoleDefinitionDto>> CreateRoleDefinitionAsync(
        CreateRoleDefinitionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<RoleDefinitionDto>> UpdateRoleDefinitionAsync(
        string key,
        UpdateRoleDefinitionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteRoleDefinitionAsync(string key, CancellationToken cancellationToken = default);

    IReadOnlyCollection<InternalDocumentSummaryDto> GetInternalDocumentIndex();

    Task<InternalDocumentDto?> GetInternalDocumentAsync(string key, CancellationToken cancellationToken = default);
}
