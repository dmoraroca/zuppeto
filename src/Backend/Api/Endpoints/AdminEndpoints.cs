using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using YepPet.Application.Admin;

namespace YepPet.Api.Endpoints;

internal static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin").RequireAuthorization();

        group.MapGet("/users", GetUsersAsync);
        group.MapPost("/users", CreateUserAsync);
        group.MapPut("/users/{id:guid}/role", UpdateUserRoleAsync);
        group.MapGet("/permissions", GetPermissionsAsync);
        group.MapPut("/permissions/{role}", UpdateRolePermissionsAsync);
        group.MapGet("/menus", GetMenusAsync);
        group.MapPut("/menus/{key}", SaveMenuAsync);
        group.MapGet("/documents", GetDocumentsAsync);
        group.MapGet("/documents/{key}", GetDocumentAsync);

        return app;
    }

    [Authorize]
    private static async Task<Results<Ok<IReadOnlyCollection<AdminUserListItemDto>>, ForbidHttpResult>> GetUsersAsync(
        ClaimsPrincipal principal,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "action.users.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(await service.GetUsersAsync(cancellationToken));
    }

    [Authorize]
    private static async Task<Results<Created<Application.Users.UserDto>, Conflict<string>, ForbidHttpResult>> CreateUserAsync(
        ClaimsPrincipal principal,
        CreateAdminUserRequest request,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "action.users.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        try
        {
            var created = await service.CreateUserAsync(request, cancellationToken);
            return TypedResults.Created($"/api/admin/users/{created.Id}", created);
        }
        catch (InvalidOperationException exception)
        {
            return TypedResults.Conflict(exception.Message);
        }
    }

    [Authorize]
    private static async Task<Results<Ok<Application.Users.UserDto>, NotFound, ForbidHttpResult>> UpdateUserRoleAsync(
        ClaimsPrincipal principal,
        Guid id,
        UpdateUserRoleRequest request,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "action.users.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var user = await service.UpdateUserRoleAsync(id, request, cancellationToken);
        return user is null ? TypedResults.NotFound() : TypedResults.Ok(user);
    }

    [Authorize]
    private static async Task<Results<Ok<RolePermissionCatalogDto>, ForbidHttpResult>> GetPermissionsAsync(
        ClaimsPrincipal principal,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "action.permissions.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(await service.GetRolePermissionsAsync(cancellationToken));
    }

    [Authorize]
    private static async Task<Results<Ok<RolePermissionCatalogDto>, ForbidHttpResult>> UpdateRolePermissionsAsync(
        ClaimsPrincipal principal,
        string role,
        UpdateRolePermissionsRequest request,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "action.permissions.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(await service.UpdateRolePermissionsAsync(request with { Role = role }, cancellationToken));
    }

    [Authorize]
    private static async Task<Results<Ok<AdminMenuCatalogDto>, ForbidHttpResult>> GetMenusAsync(
        ClaimsPrincipal principal,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "action.permissions.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(await service.GetMenusAsync(cancellationToken));
    }

    [Authorize]
    private static async Task<Results<Ok<AdminMenuCatalogDto>, ForbidHttpResult>> SaveMenuAsync(
        ClaimsPrincipal principal,
        string key,
        SaveMenuRequest request,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "action.permissions.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(await service.SaveMenuAsync(request with { Key = key }, cancellationToken));
    }

    [Authorize]
    private static async Task<Results<Ok<IReadOnlyCollection<InternalDocumentSummaryDto>>, ForbidHttpResult>> GetDocumentsAsync(
        ClaimsPrincipal principal,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "page.admin.documentation", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(service.GetInternalDocumentIndex());
    }

    [Authorize]
    private static async Task<Results<Ok<InternalDocumentDto>, NotFound, ForbidHttpResult>> GetDocumentAsync(
        ClaimsPrincipal principal,
        string key,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "page.admin.documentation", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var doc = await service.GetInternalDocumentAsync(key, cancellationToken);
        return doc is null ? TypedResults.NotFound() : TypedResults.Ok(doc);
    }
}
