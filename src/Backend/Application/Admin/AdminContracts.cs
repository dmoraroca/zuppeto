namespace YepPet.Application.Admin;

public sealed record AdminUserListItemDto(
    Guid Id,
    string Email,
    string Role,
    string DisplayName,
    bool PrivacyAccepted);

public sealed record CreateAdminUserRequest(
    string Email,
    string Password,
    string Role,
    string DisplayName);

public sealed record UpdateUserRoleRequest(string Role);

public sealed record PermissionDefinitionDto(
    string Key,
    string ScopeType,
    string DisplayName,
    string Description);

public sealed record RolePermissionAssignmentDto(
    string Role,
    string PermissionKey);

public sealed record RolePermissionCatalogDto(
    IReadOnlyCollection<PermissionDefinitionDto> Permissions,
    IReadOnlyCollection<RolePermissionAssignmentDto> Assignments);

public sealed record UpdateRolePermissionsRequest(
    string Role,
    IReadOnlyCollection<string> PermissionKeys);

public sealed record AdminMenuDefinitionDto(
    string Key,
    string Label,
    string? Route,
    string? ParentKey,
    int SortOrder,
    bool IsActive);

public sealed record MenuRoleAssignmentDto(
    string MenuKey,
    string Role);

public sealed record AdminMenuCatalogDto(
    IReadOnlyCollection<AdminMenuDefinitionDto> Menus,
    IReadOnlyCollection<MenuRoleAssignmentDto> Assignments);

public sealed record SaveMenuRequest(
    string Key,
    string Label,
    string? Route,
    string? ParentKey,
    int SortOrder,
    bool IsActive,
    IReadOnlyCollection<string> Roles);

public sealed record InternalDocumentSummaryDto(
    string Key,
    string Title);

public sealed record InternalDocumentDto(
    string Key,
    string Title,
    string Content);
