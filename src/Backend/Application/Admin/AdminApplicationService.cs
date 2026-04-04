using YepPet.Domain.Abstractions;
using YepPet.Domain.Users;

namespace YepPet.Application.Admin;

internal sealed class AdminApplicationService(
    IUserRepository userRepository,
    IRolePermissionRepository rolePermissionRepository,
    IMenuRepository menuRepository,
    Auth.IPasswordHasher passwordHasher) : IAdminApplicationService
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
                user.Role.ToString().ToUpperInvariant(),
                user.Profile.DisplayName,
                user.PrivacyConsent.Accepted))
            .ToArray();
    }

    public async Task<Users.UserDto> CreateUserAsync(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = Enum.Parse<UserRole>(request.Role, ignoreCase: true);
        var email = request.Email.Trim().ToLowerInvariant();
        var displayName = request.DisplayName.Trim();

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new InvalidOperationException($"User '{email}' already exists.");
        }

        var user = new Domain.Users.User(
            Guid.NewGuid(),
            email,
            passwordHasher.Hash(request.Password.Trim()),
            role,
            new Domain.Users.ValueObjects.UserProfile(displayName, string.Empty, string.Empty, string.Empty, null),
            new Domain.Users.ValueObjects.PrivacyConsent(false, null));

        await userRepository.AddAsync(user, cancellationToken);

        return new Users.UserDto(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.Profile.DisplayName,
            user.Profile.City,
            user.Profile.Country,
            user.Profile.Bio,
            user.Profile.AvatarUrl,
            user.PrivacyConsent.Accepted,
            user.PrivacyConsent.AcceptedAtUtc);
    }

    public async Task<Users.UserDto?> UpdateUserRoleAsync(
        Guid userId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var nextRole = Enum.Parse<UserRole>(request.Role, ignoreCase: true);
        user.ChangeRole(nextRole);
        await userRepository.UpdateAsync(user, cancellationToken);

        return new Users.UserDto(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.Profile.DisplayName,
            user.Profile.City,
            user.Profile.Country,
            user.Profile.Bio,
            user.Profile.AvatarUrl,
            user.PrivacyConsent.Accepted,
            user.PrivacyConsent.AcceptedAtUtc);
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
                    definition.Description))
                .ToArray(),
            assignments
                .Select(assignment => new RolePermissionAssignmentDto(
                    assignment.Role.ToString().ToUpperInvariant(),
                    assignment.PermissionKey))
                .ToArray());
    }

    public async Task<RolePermissionCatalogDto> UpdateRolePermissionsAsync(
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = Enum.Parse<UserRole>(request.Role, ignoreCase: true);
        await rolePermissionRepository.ReplaceRolePermissionsAsync(role, request.PermissionKeys, cancellationToken);
        return await GetRolePermissionsAsync(cancellationToken);
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
                    assignment.Role.ToUpperInvariant()))
                .ToArray());
    }

    public async Task<AdminMenuCatalogDto> SaveMenuAsync(
        SaveMenuRequest request,
        CancellationToken cancellationToken = default)
    {
        await menuRepository.SaveDefinitionAsync(
            new Domain.Navigation.MenuItemDefinition(
                request.Key.Trim(),
                request.Label.Trim(),
                string.IsNullOrWhiteSpace(request.Route) ? null : request.Route.Trim(),
                string.IsNullOrWhiteSpace(request.ParentKey) ? null : request.ParentKey.Trim(),
                request.SortOrder,
                request.IsActive),
            cancellationToken);

        await menuRepository.ReplaceMenuRolesAsync(request.Key.Trim(), request.Roles, cancellationToken);

        return await GetMenusAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionKeysByRoleAsync(
        string role,
        CancellationToken cancellationToken = default)
    {
        var parsedRole = Enum.Parse<UserRole>(role, ignoreCase: true);
        return await rolePermissionRepository.GetPermissionKeysByRoleAsync(parsedRole, cancellationToken);
    }

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
