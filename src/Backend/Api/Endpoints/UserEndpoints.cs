using Microsoft.AspNetCore.Http.HttpResults;
using YepPet.Application.Users;
using YepPet.Application.Validation;
using YepPet.Api.Validation;

namespace YepPet.Api.Endpoints;

internal static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users");

        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapGet("/by-email/{email}", GetByEmailAsync);
        group.MapPost("/", RegisterAsync);
        group.MapPut("/{id:guid}/profile", UpdateProfileAsync);

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

    private static async Task<Results<NoContent, ValidationProblem>> UpdateProfileAsync(
        Guid id,
        UserProfileUpdateRequest request,
        IValidator<UserProfileUpdateRequest> validator,
        IUserApplicationService service,
        CancellationToken cancellationToken)
    {
        var normalized = request with { Id = id };
        var validation = validator.Validate(normalized);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        await service.UpdateProfileAsync(normalized, cancellationToken);
        return TypedResults.NoContent();
    }
}
