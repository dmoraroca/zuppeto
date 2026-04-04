using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using YepPet.Application.Admin;
using YepPet.Application.Validation;
using YepPet.Api.Validation;

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
    private static async Task<Results<Created<Application.Users.UserDto>, Conflict<string>, ForbidHttpResult, ValidationProblem>> CreateUserAsync(
        ClaimsPrincipal principal,
        CreateAdminUserRequest request,
        IValidator<CreateAdminUserRequest> validator,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "action.users.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var validation = validator.Validate(request);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await service.CreateUserAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.Conflict(result.Failure?.Message ?? "Unable to create user.");
        }

        var created = result.Value!;
        return TypedResults.Created($"/api/admin/users/{created.Id}", created);
    }

    [Authorize]
    private static async Task<Results<Ok<Application.Users.UserDto>, NotFound, ForbidHttpResult, ValidationProblem>> UpdateUserRoleAsync(
        ClaimsPrincipal principal,
        Guid id,
        UpdateUserRoleRequest request,
        IValidator<UpdateUserRoleRequest> validator,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "action.users.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var validation = validator.Validate(request);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await service.UpdateUserRoleAsync(id, request, cancellationToken);

        if (!result.IsSuccess && result.Failure?.Kind == Application.Results.FailureKind.NotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(result.Value!);
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
    private static async Task<Results<Ok<RolePermissionCatalogDto>, ForbidHttpResult, ValidationProblem>> UpdateRolePermissionsAsync(
        ClaimsPrincipal principal,
        string role,
        UpdateRolePermissionsRequest request,
        IValidator<UpdateRolePermissionsRequest> validator,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "action.permissions.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var normalized = request with { Role = role };
        var validation = validator.Validate(normalized);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        return TypedResults.Ok(await service.UpdateRolePermissionsAsync(normalized, cancellationToken));
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
    private static async Task<Results<Ok<AdminMenuCatalogDto>, ForbidHttpResult, ValidationProblem>> SaveMenuAsync(
        ClaimsPrincipal principal,
        string key,
        SaveMenuRequest request,
        IValidator<SaveMenuRequest> validator,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "action.permissions.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var normalized = request with { Key = key };
        var validation = validator.Validate(normalized);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        return TypedResults.Ok(await service.SaveMenuAsync(normalized, cancellationToken));
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
