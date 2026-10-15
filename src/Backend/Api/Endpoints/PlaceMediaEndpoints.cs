using System.Security.Claims;
using Zuppeto.Application.Admin;
using Zuppeto.Application.Places;

namespace Zuppeto.Api.Endpoints;

internal static class PlaceMediaEndpoints
{
    public static IEndpointRouteBuilder MapPlaceMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/places/{placeId:guid}/media")
            .RequireAuthorization()
            .WithTags("Place media");
        group.MapGet("/", ListAsync);
        group.MapPost("/wikimedia/import", ImportWikimediaAsync);
        group.MapPost("/{assetId:guid}/primary", SetPrimaryAsync);
        group.MapPost("/{assetId:guid}/reject", RejectAsync);
        return app;
    }

    private static Task<IResult> ListAsync(Guid placeId, ClaimsPrincipal principal, IAdminApplicationService admin,
        IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAsync(principal, admin, () => service.ListAsync(placeId, cancellationToken), cancellationToken);

    private static Task<IResult> ImportWikimediaAsync(Guid placeId, ClaimsPrincipal principal, IAdminApplicationService admin,
        IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAsync(principal, admin, () => service.ImportFromWikimediaAsync(placeId, cancellationToken), cancellationToken);

    private static Task<IResult> SetPrimaryAsync(Guid placeId, Guid assetId, ClaimsPrincipal principal, IAdminApplicationService admin,
        IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAsync(principal, admin, async () => { await service.SetPrimaryAsync(placeId, assetId, cancellationToken); return true; }, cancellationToken);

    private static Task<IResult> RejectAsync(Guid placeId, Guid assetId, RejectPlaceMediaRequest request, ClaimsPrincipal principal,
        IAdminApplicationService admin, IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAsync(principal, admin, async () => { await service.RejectAsync(placeId, assetId, request.Reason, cancellationToken); return true; }, cancellationToken);

    private static async Task<IResult> ExecuteAsync<T>(ClaimsPrincipal principal, IAdminApplicationService admin,
        Func<Task<T>> action, CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(admin, "action.places.manage", cancellationToken)) return TypedResults.Forbid();
        try { return TypedResults.Ok(await action()); }
        catch (KeyNotFoundException) { return TypedResults.NotFound(); }
        catch (InvalidOperationException exception) { return TypedResults.Conflict(new { message = exception.Message }); }
    }
}
