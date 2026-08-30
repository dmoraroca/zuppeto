using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Zuppeto.Application.Auth;
using Zuppeto.Application.Users;
using Zuppeto.Application.Validation;
using Zuppeto.Api.Validation;

namespace Zuppeto.Api.Endpoints;

internal static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users");

        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapGet("/by-email/{email}", GetByEmailAsync);
        group.MapPost("/", RegisterAsync);
        group.MapPut("/{id:guid}/profile", UpdateProfileAsync).RequireAuthorization();
        group.MapPut("/{id:guid}/account", UpdateAccountAsync).RequireAuthorization();
        group.MapPost("/{id:guid}/password/verify", VerifyCurrentPasswordAsync).RequireAuthorization();

        return app;
    }

    private static async Task<Results<Ok<UserDto>, NotFound>> GetByIdAsync(
        Guid id,
        IUserApplicationService service,
        CancellationToken cancellationToken)
    {
        var user = await service.GetByIdAsync(id, cancellationToken);
        return user is null ? TypedResults.NotFound() : TypedResults.Ok(user);
    }

    private static async Task<Results<Ok<UserDto>, NotFound>> GetByEmailAsync(
        string email,
        IUserApplicationService service,
        CancellationToken cancellationToken)
    {
        var user = await service.GetByEmailAsync(email, cancellationToken);
        return user is null ? TypedResults.NotFound() : TypedResults.Ok(user);
    }

    private static async Task<Results<Created<Guid>, ValidationProblem>> RegisterAsync(
        UserRegistrationRequest request,
        IValidator<UserRegistrationRequest> validator,
        IUserApplicationService service,
        CancellationToken cancellationToken)
    {
        var validation = validator.Validate(request);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var userId = await service.RegisterAsync(request, cancellationToken);
        return TypedResults.Created($"/api/users/{userId}", userId);
    }

    private static async Task<Results<NoContent, ForbidHttpResult, ValidationProblem>> UpdateProfileAsync(
        Guid id,
        ClaimsPrincipal principal,
        UserProfileUpdateRequest request,
        IValidator<UserProfileUpdateRequest> validator,
        IUserApplicationService service,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetCurrentUserId();
        if (userId is null || userId.Value != id)
        {
            return TypedResults.Forbid();
        }

        var normalized = request with { Id = id };
        var validation = validator.Validate(normalized);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        await service.UpdateProfileAsync(normalized, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<AuthSessionDto>, ForbidHttpResult, ValidationProblem>> UpdateAccountAsync(
        Guid id,
        ClaimsPrincipal principal,
        UserAccountUpdateRequest request,
        IValidator<UserAccountUpdateRequest> validator,
        IUserApplicationService service,
        IAuthApplicationService authService,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetCurrentUserId();
        if (userId is null || userId.Value != id)
        {
            return TypedResults.Forbid();
        }

        var normalized = request with { Id = id };
        var validation = validator.Validate(normalized);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var accountResult = await service.ChangeAccountAsync(normalized, cancellationToken);
        if (!accountResult.IsValid)
        {
            return accountResult.ToValidationProblem();
        }

        var session = await authService.GetSessionByUserIdAsync(id, cancellationToken);
        if (session is null)
        {
            var notFound = ValidationResult.Success();
            notFound.Add(nameof(request.Id), "No s’ha trobat l’usuari.");
            return notFound.ToValidationProblem();
        }

        return TypedResults.Ok(session);
    }

    private static async Task<Results<Ok<UserPasswordVerifyDto>, ForbidHttpResult, NotFound>> VerifyCurrentPasswordAsync(
        Guid id,
        ClaimsPrincipal principal,
        UserPasswordVerifyRequest request,
        IUserApplicationService service,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetCurrentUserId();
        if (userId is null || userId.Value != id)
        {
            return TypedResults.Forbid();
        }

        var result = await service.VerifyCurrentPasswordAsync(id, request.Password ?? string.Empty, cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
