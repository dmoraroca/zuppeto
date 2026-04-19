namespace YepPet.Application.Admin;

public sealed record AdminUserListItemDto(
    Guid Id,
    string Email,
    string Role,
    string DisplayName,
    string City,
    string Country,
    string Bio,
    string? AvatarUrl,
    bool PrivacyAccepted,
    DateTimeOffset? PrivacyAcceptedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastAccessedAtUtc);

public sealed record CreateAdminUserRequest(
    string Email,
    string Password,
    string Role,
    string DisplayName,
    string City,
    string Country,
    string? AvatarUrl);

public sealed record UpdateUserRoleRequest(string Role);

public sealed record PermissionDefinitionDto(
    string Key,
    string ScopeType,
    string DisplayName,
    string Description,
    string? ScopePayload,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record RolePermissionAssignmentDto(
    string Role,
    string PermissionKey);

public sealed record RolePermissionCatalogDto(
    IReadOnlyCollection<PermissionDefinitionDto> Permissions,
    IReadOnlyCollection<RolePermissionAssignmentDto> Assignments);

public sealed record UpdateRolePermissionsRequest(
    string Role,
    IReadOnlyCollection<string> PermissionKeys);

/// <summary>Crea un permís al catàleg amb la seva clau interna i metadades.</summary>
public sealed record CreatePermissionDefinitionRequest(
    string Key,
    string DisplayName,
    string Description,
    string ScopeType,
    string? ScopePayload);

/// <summary>Actualitza metadades del permís (la clau no es pot canviar).</summary>
public sealed record UpdatePermissionDefinitionRequest(
    string DisplayName,
    string Description,
    string ScopeType,
    string? ScopePayload);

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

public sealed record RoleDefinitionDto(
    Guid Id,
    string Key,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateRoleDefinitionRequest(string Key, string DisplayName);

public sealed record UpdateRoleDefinitionRequest(string DisplayName, bool IsActive);
