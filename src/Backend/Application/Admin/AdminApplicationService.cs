using YepPet.Domain.Abstractions;
using YepPet.Domain.Roles;
using YepPet.Application.Admin.Commands;
using YepPet.Application.Commands;
using YepPet.Application.Factories;
using YepPet.Application.Results;
using YepPet.Domain.Permissions;

namespace YepPet.Application.Admin;

internal sealed class AdminApplicationService(
    IUserRepository userRepository,
    IRolePermissionRepository rolePermissionRepository,
    IRoleCatalogRepository roleCatalogRepository,
    IMenuRepository menuRepository,
    ICommandHandler<CreateAdminUserCommand, Result<Users.UserDto>> createUserHandler,
    ICommandHandler<UpdateUserRoleCommand, Result<Users.UserDto>> updateRoleHandler,
    IMenuItemDefinitionFactory menuItemDefinitionFactory) : IAdminApplicationService
{
    private static readonly InternalDocumentSummaryDto[] InternalDocuments =
    [
        new("project-phases", "Fases del projecte"),
        new("tecnic-ca", "Documentació tècnica"),
        new("funcional-ca", "Documentació funcional"),
        new("auth-ca", "Autenticació")
    ];

    public async Task<IReadOnlyCollection<AdminUserListItemDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.ListAsync(cancellationToken);
        return users
            .Select(user => new AdminUserListItemDto(
                user.Id,
                user.Email,
                user.Role,
                user.Profile.DisplayName,
                user.Profile.City,
                user.Profile.Country,
                user.Profile.Bio,
                user.Profile.AvatarUrl,
                user.PrivacyConsent.Accepted,
                user.PrivacyConsent.AcceptedAtUtc,
                user.CreatedAtUtc,
                user.LastAccessedAtUtc))
            .ToArray();
    }

    public async Task<Result<Users.UserDto>> CreateUserAsync(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken = default)
    {
        return await createUserHandler.HandleAsync(new CreateAdminUserCommand(request), cancellationToken);
    }

    public async Task<Result<Users.UserDto>> UpdateUserRoleAsync(
        Guid userId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        return await updateRoleHandler.HandleAsync(new UpdateUserRoleCommand(userId, request), cancellationToken);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await userRepository.DeleteAsync(userId, cancellationToken);
    }

    public async Task<RolePermissionCatalogDto> GetRolePermissionsAsync(CancellationToken cancellationToken = default)
    {
        var definitions = await rolePermissionRepository.GetDefinitionsAsync(cancellationToken);
        var assignments = await rolePermissionRepository.GetAssignmentsAsync(cancellationToken);

        return new RolePermissionCatalogDto(
            definitions
                .Select(definition => new PermissionDefinitionDto(
                    definition.Key,
                    definition.ScopeType,
                    definition.DisplayName,
                    definition.Description,
                    definition.ScopePayload,
                    definition.CreatedAtUtc,
                    definition.UpdatedAtUtc))
                .ToArray(),
            assignments
                .Select(assignment => new RolePermissionAssignmentDto(
                    assignment.Role,
                    assignment.PermissionKey))
                .ToArray());
    }

    public async Task<Result<RolePermissionCatalogDto>> UpdateRolePermissionsAsync(
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var row = await roleCatalogRepository.GetByKeyAsync(request.Role.Trim(), cancellationToken);
        if (row is null || !row.IsActive)
        {
            return Result<RolePermissionCatalogDto>.Fail(
                FailureKind.Conflict,
                "Rol invàlid o inactiu.");
        }

        await rolePermissionRepository.ReplaceRolePermissionsAsync(row.Key, request.PermissionKeys, cancellationToken);
        return Result<RolePermissionCatalogDto>.Success(await GetRolePermissionsAsync(cancellationToken));
    }

    public async Task<Result<PermissionDefinitionDto>> CreatePermissionDefinitionAsync(
        CreatePermissionDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        var key = request.Key.Trim();
        var displayName = request.DisplayName.Trim();
        var description = request.Description?.Trim() ?? string.Empty;
        var scope = NormalizePermissionScopeType(request.ScopeType);
        var definitions = await rolePermissionRepository.GetDefinitionsAsync(cancellationToken);
        if (definitions.Any(d =>
                string.Equals(d.Key.Trim(), key, StringComparison.OrdinalIgnoreCase)))
        {
            return Result<PermissionDefinitionDto>.Fail(
                FailureKind.Conflict,
                "Ja existeix un permís amb aquesta clau interna al catàleg.");
        }

        if (definitions.Any(d =>
                string.Equals(d.DisplayName.Trim(), displayName, StringComparison.OrdinalIgnoreCase)))
        {
            return Result<PermissionDefinitionDto>.Fail(
                FailureKind.Conflict,
                "Ja existeix un permís amb aquest nom visible al catàleg.");
        }

        if (!PermissionScopePayloadRules.TryNormalize(scope, request.ScopePayload, out var normalizedScopePayload, out var scopePayloadError))
        {
            return Result<PermissionDefinitionDto>.Fail(
                FailureKind.Conflict,
                scopePayloadError);
        }

        var definition = new PermissionDefinition(
            key,
            scope,
            displayName,
            description,
            normalizedScopePayload,
            default,
            default);

        var created = await rolePermissionRepository.AddPermissionDefinitionAsync(definition, cancellationToken);

        return Result<PermissionDefinitionDto>.Success(new PermissionDefinitionDto(
            created.Key,
            created.ScopeType,
            created.DisplayName,
            created.Description,
            created.ScopePayload,
            created.CreatedAtUtc,
            created.UpdatedAtUtc));
    }

    public async Task<Result<bool>> DeletePermissionDefinitionAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalized = key.Trim();
        var deleted = await rolePermissionRepository.DeletePermissionDefinitionAsync(normalized, cancellationToken);
        return deleted
            ? Result<bool>.Success(true)
            : Result<bool>.Fail(FailureKind.NotFound, "Permís no trobat.");
    }

    public async Task<Result<PermissionDefinitionDto>> UpdatePermissionDefinitionAsync(
        string key,
        UpdatePermissionDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = key.Trim();
        var scope = NormalizePermissionScopeType(request.ScopeType);
        var displayName = request.DisplayName?.Trim() ?? string.Empty;
        var description = request.Description?.Trim() ?? string.Empty;

        if (!PermissionScopePayloadRules.TryNormalize(scope, request.ScopePayload, out var normalizedScopePayload, out var scopePayloadError))
        {
            return Result<PermissionDefinitionDto>.Fail(
                FailureKind.Conflict,
                scopePayloadError);
        }

        var updated = await rolePermissionRepository.UpdatePermissionDefinitionAsync(
            normalizedKey,
            scope,
            displayName,
            description,
            normalizedScopePayload,
            cancellationToken);

        if (updated is null)
        {
            return Result<PermissionDefinitionDto>.Fail(FailureKind.NotFound, "Permís no trobat.");
        }

        return Result<PermissionDefinitionDto>.Success(new PermissionDefinitionDto(
            updated.Key,
            updated.ScopeType,
            updated.DisplayName,
            updated.Description,
            updated.ScopePayload,
            updated.CreatedAtUtc,
            updated.UpdatedAtUtc));
    }

    private static string NormalizePermissionScopeType(string? scopeType)
    {
        return (scopeType ?? string.Empty).Trim().ToLowerInvariant();
    }

    public async Task<AdminMenuCatalogDto> GetMenusAsync(CancellationToken cancellationToken = default)
    {
        var definitions = await menuRepository.GetDefinitionsAsync(cancellationToken);
        var assignments = await menuRepository.GetAssignmentsAsync(cancellationToken);

        return new AdminMenuCatalogDto(
            definitions
                .Select(definition => new AdminMenuDefinitionDto(
                    definition.Key,
                    definition.Label,
                    definition.Route,
                    definition.ParentKey,
                    definition.SortOrder,
                    definition.IsActive))
                .ToArray(),
            assignments
                .Select(assignment => new MenuRoleAssignmentDto(
                    assignment.MenuKey,
                    assignment.Role))
                .ToArray());
    }

    public async Task<AdminMenuCatalogDto> SaveMenuAsync(
        SaveMenuRequest request,
        CancellationToken cancellationToken = default)
    {
        await menuRepository.SaveDefinitionAsync(
            menuItemDefinitionFactory.Create(request),
            cancellationToken);

        await menuRepository.ReplaceMenuRolesAsync(request.Key.Trim(), request.Roles, cancellationToken);

        return await GetMenusAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionKeysByRoleAsync(
        string role,
        CancellationToken cancellationToken = default)
    {
        var row = await roleCatalogRepository.GetByKeyAsync(role.Trim(), cancellationToken);
        if (row is null || !row.IsActive)
        {
            return Array.Empty<string>();
        }

        return await rolePermissionRepository.GetPermissionKeysByRoleAsync(row.Key, cancellationToken);
    }

    public async Task<IReadOnlyCollection<RoleDefinitionDto>> GetRoleDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await roleCatalogRepository.ListAsync(cancellationToken);
        return rows.Select(ToRoleDefinitionDto).ToArray();
    }

    public async Task<Result<RoleDefinitionDto>> CreateRoleDefinitionAsync(
        CreateRoleDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await roleCatalogRepository.KeyExistsAsync(request.Key, cancellationToken))
        {
            return Result<RoleDefinitionDto>.Fail(
                FailureKind.Conflict,
                "Ja existeix un rol amb aquesta clau.");
        }

        var row = await roleCatalogRepository.CreateAsync(request.Key, request.DisplayName, cancellationToken);
        return Result<RoleDefinitionDto>.Success(ToRoleDefinitionDto(row));
    }

    public async Task<Result<RoleDefinitionDto>> UpdateRoleDefinitionAsync(
        string key,
        UpdateRoleDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await roleCatalogRepository.UpdateAsync(
            key,
            request.DisplayName,
            request.IsActive,
            cancellationToken);

        if (updated is null)
        {
            return Result<RoleDefinitionDto>.Fail(FailureKind.NotFound, "Rol no trobat.");
        }

        return Result<RoleDefinitionDto>.Success(ToRoleDefinitionDto(updated));
    }

    public async Task<Result<bool>> DeleteRoleDefinitionAsync(string key, CancellationToken cancellationToken = default)
    {
        var outcome = await roleCatalogRepository.TryDeleteAsync(key, cancellationToken);
        return outcome switch
        {
            RoleDeleteOutcome.Deleted => Result<bool>.Success(true),
            RoleDeleteOutcome.NotFound => Result<bool>.Fail(FailureKind.NotFound, "Rol no trobat."),
            RoleDeleteOutcome.HasDependencies => Result<bool>.Fail(
                FailureKind.Conflict,
                "No es pot esborrar: el rol té usuaris o assignacions associades."),
            _ => Result<bool>.Fail(FailureKind.Conflict, "No s'ha pogut esborrar el rol.")
        };
    }

    private static RoleDefinitionDto ToRoleDefinitionDto(RoleCatalogRow row) =>
        new(row.Id, row.Key, row.DisplayName, row.IsActive, row.CreatedAtUtc, row.UpdatedAtUtc);

    public IReadOnlyCollection<InternalDocumentSummaryDto> GetInternalDocumentIndex()
    {
        return InternalDocuments;
    }

    public async Task<InternalDocumentDto?> GetInternalDocumentAsync(string key, CancellationToken cancellationToken = default)
    {
        var doc = InternalDocuments.FirstOrDefault(current => current.Key == key);

        if (doc is null)
        {
            return null;
        }

        var relativePath = key switch
        {
            "project-phases" => Path.Combine("docs", "project-phases.md"),
            "tecnic-ca" => Path.Combine("docs", "ca", "tecnic-ca.md"),
            "funcional-ca" => Path.Combine("docs", "ca", "funcional-ca.md"),
            "auth-ca" => Path.Combine("docs", "ca", "auth-ca.md"),
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var fullPath = Path.Combine(repoRoot, relativePath);

        if (!File.Exists(fullPath))
        {
            return null;
        }

        var content = await File.ReadAllTextAsync(fullPath, cancellationToken);
        return new InternalDocumentDto(doc.Key, doc.Title, content);
    }
}
