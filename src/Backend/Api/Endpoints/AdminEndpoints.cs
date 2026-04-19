using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using YepPet.Application.Admin;
using YepPet.Application.Results;
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
        group.MapDelete("/users/{id:guid}", DeleteUserAsync);
        group.MapGet("/permissions", GetPermissionsAsync);
        group.MapPost("/permissions/definitions", CreatePermissionDefinitionAsync);
        // Alias sense col·lisió amb /permissions/{role} antic (role=definitions → 405 en POST).
        group.MapPost("/permissions/definitions/create", CreatePermissionDefinitionAsync);
        group.MapPut("/permissions/definitions/{key}", UpdatePermissionDefinitionAsync);
        group.MapDelete("/permissions/definitions/{key}", DeletePermissionDefinitionAsync);
        // /permissions/{role} col·lisionava amb /permissions/definitions (role=definitions) i donava 405 en POST.
        group.MapPut("/permissions/roles/{role}", UpdateRolePermissionsAsync);
        // Compatibilitat clients antics: mateix handler; rols explícits perquè "definitions" no coincideixi.
        // Legacy URL (case-insensitive) for the four built-in role keys; prefer /permissions/roles/{role}.
        group.MapPut("/permissions/{role:regex(^(?i)(admin|user|viewer|developer)$)}", UpdateRolePermissionsAsync);
        group.MapGet("/menus", GetMenusAsync);
        group.MapPut("/menus/{key}", SaveMenuAsync);
        group.MapGet("/roles", GetRoleDefinitionsAsync);
        group.MapPost("/roles", CreateRoleDefinitionAsync);
        group.MapPut("/roles/{key}", UpdateRoleDefinitionAsync);
        group.MapDelete("/roles/{key}", DeleteRoleDefinitionAsync);
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
    private static async Task<Results<NoContent, ForbidHttpResult>> DeleteUserAsync(
        ClaimsPrincipal principal,
        Guid id,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "action.users.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        await service.DeleteUserAsync(id, cancellationToken);
        return TypedResults.NoContent();
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
    private static async Task<Results<Ok<RolePermissionCatalogDto>, Conflict<string>, ForbidHttpResult, ValidationProblem>> UpdateRolePermissionsAsync(
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

        var outcome = await service.UpdateRolePermissionsAsync(normalized, cancellationToken);
        if (!outcome.IsSuccess)
        {
            return TypedResults.Conflict(outcome.Failure?.Message ?? "Unable to update role permissions.");
        }

        return TypedResults.Ok(outcome.Value!);
    }

    [Authorize]
    private static async Task<Results<Ok<PermissionDefinitionDto>, Conflict<string>, ForbidHttpResult, ValidationProblem>> CreatePermissionDefinitionAsync(
        ClaimsPrincipal principal,
        CreatePermissionDefinitionRequest request,
        IValidator<CreatePermissionDefinitionRequest> validator,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "action.permissions.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var validation = validator.Validate(request);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await service.CreatePermissionDefinitionAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.Conflict(result.Failure?.Message ?? "No s'ha pogut crear el permís.");
        }

        return TypedResults.Ok(result.Value!);
    }

    [Authorize]
    private static async Task<Results<Ok<PermissionDefinitionDto>, NotFound, Conflict<string>, ForbidHttpResult, ValidationProblem>> UpdatePermissionDefinitionAsync(
        ClaimsPrincipal principal,
        string key,
        UpdatePermissionDefinitionRequest request,
        IValidator<UpdatePermissionDefinitionRequest> validator,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "action.permissions.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var validation = validator.Validate(request);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await service.UpdatePermissionDefinitionAsync(key, request, cancellationToken);
        if (!result.IsSuccess && result.Failure?.Kind == FailureKind.NotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.Conflict(result.Failure?.Message ?? "No s'ha pogut actualitzar el permís.");
        }

        return TypedResults.Ok(result.Value!);
    }

    [Authorize]
    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> DeletePermissionDefinitionAsync(
        ClaimsPrincipal principal,
        string key,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "action.permissions.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var result = await service.DeletePermissionDefinitionAsync(key, cancellationToken);
        if (!result.IsSuccess && result.Failure?.Kind == FailureKind.NotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }

    [Authorize]
    private static async Task<Results<Ok<IReadOnlyCollection<RoleDefinitionDto>>, ForbidHttpResult>> GetRoleDefinitionsAsync(
        ClaimsPrincipal principal,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "page.admin.roles", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(await service.GetRoleDefinitionsAsync(cancellationToken));
    }

    [Authorize]
    private static async Task<Results<Ok<RoleDefinitionDto>, Conflict<string>, ForbidHttpResult, ValidationProblem>> CreateRoleDefinitionAsync(
        ClaimsPrincipal principal,
        CreateRoleDefinitionRequest request,
        IValidator<CreateRoleDefinitionRequest> validator,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "page.admin.roles", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var validation = validator.Validate(request);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await service.CreateRoleDefinitionAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.Conflict(result.Failure?.Message ?? "Unable to create role.");
        }

        return TypedResults.Ok(result.Value!);
    }

    [Authorize]
    private static async Task<Results<Ok<RoleDefinitionDto>, NotFound, Conflict<string>, ForbidHttpResult, ValidationProblem>> UpdateRoleDefinitionAsync(
        ClaimsPrincipal principal,
        string key,
        UpdateRoleDefinitionRequest request,
        IValidator<UpdateRoleDefinitionRequest> validator,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "page.admin.roles", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var validation = validator.Validate(request);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await service.UpdateRoleDefinitionAsync(key, request, cancellationToken);
        if (!result.IsSuccess && result.Failure?.Kind == FailureKind.NotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.Conflict(result.Failure?.Message ?? "Unable to update role.");
        }

        return TypedResults.Ok(result.Value!);
    }

    [Authorize]
    private static async Task<Results<NoContent, NotFound, Conflict<string>, ForbidHttpResult>> DeleteRoleDefinitionAsync(
        ClaimsPrincipal principal,
        string key,
        IAdminApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(service, "page.admin.roles", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var result = await service.DeleteRoleDefinitionAsync(key, cancellationToken);
        if (!result.IsSuccess && result.Failure?.Kind == FailureKind.NotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.Conflict(result.Failure?.Message ?? "Unable to delete role.");
        }

        return TypedResults.NoContent();
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
