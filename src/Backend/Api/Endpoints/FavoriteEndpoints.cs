using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Zuppeto.Application.Admin;
using Zuppeto.Application.Favorites;

namespace Zuppeto.Api.Endpoints;

internal static class FavoriteEndpoints
{
    public static IEndpointRouteBuilder MapFavoriteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/favorites").RequireAuthorization();

        group.MapGet("/{ownerUserId:guid}", GetByOwnerAsync);
        group.MapPost("/{ownerUserId:guid}/places/{placeId:guid}", SavePlaceAsync);
        group.MapDelete("/{ownerUserId:guid}/places/{placeId:guid}", RemovePlaceAsync);

        return app;
    }

    private static bool IsFavoriteOwner(ClaimsPrincipal principal, Guid ownerUserId) =>
        principal.GetCurrentUserId() is { } uid && uid == ownerUserId;

    private static async Task<Results<Ok<FavoriteListDto>, ForbidHttpResult>> GetByOwnerAsync(
        Guid ownerUserId,
        ClaimsPrincipal principal,
        IFavoriteListApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!IsFavoriteOwner(principal, ownerUserId))
        {
            return TypedResults.Forbid();
        }

        var favoriteList = await service.GetByOwnerAsync(ownerUserId, cancellationToken);
        return TypedResults.Ok(
            favoriteList ?? new FavoriteListDto(Guid.Empty, ownerUserId, Array.Empty<FavoriteEntryDto>()));
    }

    private static async Task<Results<Ok<Guid>, ForbidHttpResult>> SavePlaceAsync(
        Guid ownerUserId,
        Guid placeId,
        ClaimsPrincipal principal,
        IAdminApplicationService adminService,
        IFavoriteListApplicationService service,
        CancellationToken cancellationToken)
    {
        var isViewer = string.Equals(
            principal.GetCurrentRoleKey(),
            "Viewer",
            StringComparison.OrdinalIgnoreCase);
        if (!IsFavoriteOwner(principal, ownerUserId)
            || isViewer
            || !await principal.HasPermissionAsync(
                adminService,
                "action.favorites.write",
                cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var favoriteListId = await service.SavePlaceAsync(ownerUserId, placeId, cancellationToken);
        return TypedResults.Ok(favoriteListId);
    }

    private static async Task<Results<NoContent, ForbidHttpResult>> RemovePlaceAsync(
        Guid ownerUserId,
        Guid placeId,
        ClaimsPrincipal principal,
        IAdminApplicationService adminService,
        IFavoriteListApplicationService service,
        CancellationToken cancellationToken)
    {
        var isViewer = string.Equals(
            principal.GetCurrentRoleKey(),
            "Viewer",
            StringComparison.OrdinalIgnoreCase);
        if (!IsFavoriteOwner(principal, ownerUserId)
            || isViewer
            || !await principal.HasPermissionAsync(
                adminService,
                "action.favorites.write",
                cancellationToken))
        {
            return TypedResults.Forbid();
        }

        await service.RemovePlaceAsync(ownerUserId, placeId, cancellationToken);
        return TypedResults.NoContent();
    }
}
