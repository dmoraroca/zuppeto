using Microsoft.EntityFrameworkCore;
using YepPet.Application.Auth;
using YepPet.Infrastructure.Persistence;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Auth;

public sealed class DevelopmentIdentitySeeder(
    YepPetDbContext dbContext,
    IPasswordHasher passwordHasher)
{
    private static readonly (string Key, string DisplayName)[] RoleSeeds =
    [
        ("Viewer", "Viewer"),
        ("User", "User"),
        ("Developer", "Developer"),
        ("Admin", "Admin")
    ];

    private static readonly PermissionSeed[] PermissionSeeds =
    [
        new("menu.admin", "menu", "Menú ADMIN", "Accés al contenidor del menú d'administració."),
        new("menu.admin.documentation", "menu", "Menú Documentació", "Accés a l'opció de documentació interna."),
        new("menu.admin.users", "menu", "Menú Usuaris", "Accés al manteniment intern d'usuaris."),
        new("menu.admin.permissions", "menu", "Menú Permisos", "Accés al manteniment intern de permisos i rols."),
        new("menu.admin.roles", "menu", "Menú Rols", "Accés al manteniment del catàleg de rols."),
        new("menu.admin.places", "menu", "Menú Llocs", "Accés al manteniment intern de llocs."),
        new("page.home", "page", "Inici", "Accés a la pantalla principal."),
        new("page.places", "page", "Llocs", "Accés al llistat de llocs."),
        new("page.place-detail", "page", "Detall de lloc", "Accés al detall d'un lloc."),
        new("page.favorites", "page", "Favorits", "Accés a la pantalla de favorits."),
        new("page.profile", "page", "Perfil", "Accés a la pantalla de perfil."),
        new("page.admin.documentation", "page", "Documentació interna", "Accés a la documentació interna en format Markdown."),
        new("page.admin.users", "page", "Usuaris", "Accés al manteniment intern d'usuaris."),
        new("page.admin.permissions", "page", "Permisos", "Accés al manteniment intern de permisos i rols."),
        new("page.admin.roles", "page", "Rols (admin)", "Accés al manteniment del catàleg de rols."),
        new("page.admin.places", "page", "Llocs (admin)", "Accés al manteniment intern de llocs."),
        new("menu.admin.countries", "menu", "Menú Països", "Accés al manteniment del catàleg de països."),
        new("menu.admin.cities", "menu", "Menú Ciutats", "Accés al manteniment del catàleg de ciutats."),
        new("page.admin.countries", "page", "Països (admin)", "Accés al manteniment del catàleg de països."),
        new("page.admin.cities", "page", "Ciutats (admin)", "Accés al manteniment del catàleg de ciutats."),
        new("action.favorites.write", "action", "Editar favorits", "Permet afegir o eliminar favorits."),
        new("action.profile.write", "action", "Editar perfil", "Permet actualitzar dades del perfil propi."),
        new("action.users.manage", "action", "Gestionar usuaris", "Permet crear, editar o assignar rols a usuaris."),
        new("action.permissions.manage", "action", "Gestionar permisos", "Permet modificar permisos associats a cada rol."),
        new("action.places.manage", "action", "Gestionar llocs", "Permet crear, editar i esborrar llocs del catàleg intern."),
        new("action.geographic.manage", "action", "Gestionar catàleg geogràfic", "Permet crear, editar i esborrar països i ciutats del catàleg intern.")
    ];

    private static readonly IReadOnlyDictionary<string, string[]> RolePermissionSeeds =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Viewer"] =
            [
                "page.home",
                "page.places",
                "page.place-detail",
                "page.favorites",
                "page.profile"
            ],
            ["User"] =
            [
                "page.home",
                "page.places",
                "page.place-detail",
                "page.favorites",
                "page.profile",
                "action.favorites.write",
                "action.profile.write"
            ],
            ["Developer"] =
            [
                "menu.admin",
                "menu.admin.documentation",
                "page.home",
                "page.places",
                "page.place-detail",
                "page.favorites",
                "page.profile",
                "page.admin.documentation",
                "action.favorites.write",
                "action.profile.write"
            ],
            ["Admin"] =
            [
                "menu.admin",
                "menu.admin.documentation",
                "menu.admin.users",
                "menu.admin.permissions",
                "menu.admin.roles",
                "menu.admin.places",
                "menu.admin.countries",
                "menu.admin.cities",
                "page.home",
                "page.places",
                "page.place-detail",
                "page.favorites",
                "page.profile",
                "page.admin.documentation",
                "page.admin.users",
                "page.admin.permissions",
                "page.admin.roles",
                "page.admin.places",
                "page.admin.countries",
                "page.admin.cities",
                "action.favorites.write",
                "action.profile.write",
                "action.users.manage",
                "action.permissions.manage",
                "action.places.manage",
                "action.geographic.manage"
            ]
        };

    private static readonly MenuSeed[] MenuSeeds =
    [
        new("home", "Inici", "/", null, 10, true),
        new("places", "Llocs", "/places", null, 20, true),
        new("favorites", "Favorits", "/favorites", null, 30, true),
        new("profile", "Perfil", "/perfil", null, 40, true),
        new("help", "Ajuda", null, null, 50, true),
        new("help.general", "Com funciona", "/ajuda", "help", 10, true),
        new("help.contact", "Contacta'ns", "/contacte", "help", 20, true),
        new("admin", "Del administrador", null, null, 60, true),
        new("admin.documentation", "Documentació", "/admin/documentacio", "admin", 10, true),
        new("admin.users", "Usuaris", "/admin/usuaris", "admin", 20, true),
        new("admin.permissions", "Permisos", "/admin/permisos", "admin", 30, true),
        new("admin.roles", "Rols", "/admin/rols", "admin", 35, true),
        new("admin.menus", "Menús", "/admin/menus", "admin", 40, true),
        new("admin.places", "Llocs", "/admin/llocs", "admin", 50, true),
        new("admin.countries", "Països", "/admin/paisos", "admin", 60, true),
        new("admin.cities", "Ciutats", "/admin/ciutats", "admin", 70, true)
    ];

    private static readonly IReadOnlyDictionary<string, string[]> MenuRoleSeeds =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Viewer"] =
            [
                "home",
                "places",
                "favorites",
                "profile",
                "help",
                "help.general",
                "help.contact"
            ],
            ["User"] =
            [
                "home",
                "places",
                "favorites",
                "profile",
                "help",
                "help.general",
                "help.contact"
            ],
            ["Developer"] =
            [
                "home",
                "places",
                "favorites",
                "profile",
                "help",
                "help.general",
                "help.contact",
                "admin",
                "admin.documentation"
            ],
            ["Admin"] =
            [
                "home",
                "places",
                "favorites",
                "profile",
                "help",
                "help.general",
                "help.contact",
                "admin",
                "admin.documentation",
                "admin.users",
                "admin.permissions",
                "admin.roles",
                "admin.menus",
                "admin.places",
                "admin.countries",
                "admin.cities"
            ]
        };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRolesAsync(cancellationToken);
        await EnsurePermissionCatalogAsync(cancellationToken);
        await EnsureMenuCatalogAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await EnsureUserAsync(
            email: "admin@admin.adm",
            password: "Admin123",
            role: "Admin",
            displayName: "Administrador YepPet",
            city: "Barcelona",
            country: "Espanya",
            bio: "Accés intern per revisar arquitectura, permisos i evolució del producte.",
            privacyAccepted: true,
            cancellationToken);

        await EnsureUserAsync(
            email: "user@user.com",
            password: "Admin123",
            role: "User",
            displayName: "Usuari de prova",
            city: "Madrid",
            country: "Espanya",
            bio: "Usuari local de desenvolupament per validar autenticació, sessió i perfil real.",
            privacyAccepted: true,
            cancellationToken);

        await EnsureRolePermissionsAsync(cancellationToken);
        await EnsureMenuRolesAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureRolesAsync(CancellationToken cancellationToken)
    {
        var existing = await dbContext.Roles
            .ToDictionaryAsync(role => role.Key, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var (key, displayName) in RoleSeeds)
        {
            if (existing.TryGetValue(key, out var record))
            {
                record.DisplayName = displayName;
                record.IsActive = true;
                continue;
            }

            dbContext.Roles.Add(new RoleRecord
            {
                Id = Guid.NewGuid(),
                Key = key,
                DisplayName = displayName,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
    }

    private async Task EnsurePermissionCatalogAsync(CancellationToken cancellationToken)
    {
        var existing = await dbContext.Permissions
            .ToDictionaryAsync(permission => permission.Key, StringComparer.Ordinal, cancellationToken);

        foreach (var seed in PermissionSeeds)
        {
            if (existing.TryGetValue(seed.Key, out var record))
            {
                var changed =
                    !string.Equals(record.ScopeType, seed.ScopeType, StringComparison.Ordinal)
                    || !string.Equals(record.DisplayName, seed.DisplayName, StringComparison.Ordinal)
                    || !string.Equals(record.Description, seed.Description, StringComparison.Ordinal);

                record.ScopeType = seed.ScopeType;
                record.DisplayName = seed.DisplayName;
                record.Description = seed.Description;
                if (changed)
                {
                    record.UpdatedAtUtc = DateTimeOffset.UtcNow;
                }

                continue;
            }

            var now = DateTimeOffset.UtcNow;
            dbContext.Permissions.Add(new PermissionRecord
            {
                Id = Guid.NewGuid(),
                Key = seed.Key,
                ScopeType = seed.ScopeType,
                DisplayName = seed.DisplayName,
                Description = seed.Description,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
    }

    private async Task EnsureUserAsync(
        string email,
        string password,
        string role,
        string displayName,
        string city,
        string country,
        string bio,
        bool privacyAccepted,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await dbContext.Users.FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (existing is not null)
        {
            if (!existing.PasswordHash.StartsWith("pbkdf2$", StringComparison.OrdinalIgnoreCase))
            {
                existing.PasswordHash = passwordHasher.Hash(password);
            }

            if (!string.Equals(existing.Role, role, StringComparison.OrdinalIgnoreCase))
            {
                existing.Role = role;
            }

            if (privacyAccepted && !existing.PrivacyAccepted)
            {
                existing.PrivacyAccepted = true;
                existing.PrivacyAcceptedAtUtc ??= DateTimeOffset.UtcNow;
            }

            return;
        }

        dbContext.Users.Add(new UserRecord
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(password),
            Role = role,
            DisplayName = displayName,
            City = city,
            Country = country,
            Bio = bio,
            AvatarUrl = null,
            PrivacyAccepted = privacyAccepted,
            PrivacyAcceptedAtUtc = privacyAccepted ? DateTimeOffset.UtcNow : null
        });
    }

    private async Task EnsureRolePermissionsAsync(CancellationToken cancellationToken)
    {
        var permissions = await dbContext.Permissions
            .Select(permission => permission.Key)
            .ToListAsync(cancellationToken);
        var validPermissionKeys = permissions.ToHashSet(StringComparer.Ordinal);

        var existing = await dbContext.RolePermissions.ToListAsync(cancellationToken);
        dbContext.RolePermissions.RemoveRange(existing);

        foreach (var (role, permissionKeys) in RolePermissionSeeds)
        {
            foreach (var permissionKey in permissionKeys)
            {
                if (!validPermissionKeys.Contains(permissionKey))
                {
                    continue;
                }

                dbContext.RolePermissions.Add(new RolePermissionRecord
                {
                    Id = Guid.NewGuid(),
                    Role = role,
                    PermissionKey = permissionKey
                });
            }
        }
    }

    private async Task EnsureMenuCatalogAsync(CancellationToken cancellationToken)
    {
        var existing = await dbContext.Menus
            .ToDictionaryAsync(menu => menu.Key, StringComparer.Ordinal, cancellationToken);

        foreach (var seed in MenuSeeds)
        {
            if (existing.TryGetValue(seed.Key, out var record))
            {
                record.Label = seed.Label;
                record.Route = seed.Route;
                record.ParentKey = seed.ParentKey;
                record.SortOrder = seed.SortOrder;
                record.IsActive = seed.IsActive;
                continue;
            }

            dbContext.Menus.Add(new MenuRecord
            {
                Id = Guid.NewGuid(),
                Key = seed.Key,
                Label = seed.Label,
                Route = seed.Route,
                ParentKey = seed.ParentKey,
                SortOrder = seed.SortOrder,
                IsActive = seed.IsActive
            });
        }
    }

    private async Task EnsureMenuRolesAsync(CancellationToken cancellationToken)
    {
        var menuKeys = await dbContext.Menus
            .Select(menu => menu.Key)
            .ToListAsync(cancellationToken);
        var validMenuKeys = menuKeys.ToHashSet(StringComparer.Ordinal);

        var existing = await dbContext.MenuRoles.ToListAsync(cancellationToken);
        dbContext.MenuRoles.RemoveRange(existing);

        foreach (var (role, menuKeysForRole) in MenuRoleSeeds)
        {
            foreach (var menuKey in menuKeysForRole)
            {
                if (!validMenuKeys.Contains(menuKey))
                {
                    continue;
                }

                dbContext.MenuRoles.Add(new MenuRoleRecord
                {
                    Id = Guid.NewGuid(),
                    Role = role,
                    MenuKey = menuKey
                });
            }
        }
    }

    private sealed record PermissionSeed(
        string Key,
        string ScopeType,
        string DisplayName,
        string Description);

    private sealed record MenuSeed(
        string Key,
        string Label,
        string? Route,
        string? ParentKey,
        int SortOrder,
        bool IsActive);
}
