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
        group.MapGet("/mapillary/candidates", SearchMapillaryAsync);
        group.MapPost("/upload", UploadAsync).DisableAntiforgery();
        group.MapPost("/{assetId:guid}/accept", AcceptAsync);
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

    private static Task<IResult> SearchMapillaryAsync(Guid placeId, ClaimsPrincipal principal, IAdminApplicationService admin,
        IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAsync(principal, admin, () => service.SearchMapillaryAsync(placeId, cancellationToken), cancellationToken);

    private static Task<IResult> SetPrimaryAsync(Guid placeId, Guid assetId, ClaimsPrincipal principal, IAdminApplicationService admin,
        IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAsync(principal, admin, async () => { await service.SetPrimaryAsync(placeId, assetId, cancellationToken); return true; }, cancellationToken);

    private static async Task<IResult> UploadAsync(
        Guid placeId,
        HttpRequest request,
        ClaimsPrincipal principal,
        IAdminApplicationService admin,
        IPlaceMediaApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(principal.GetCurrentRoleKey(), "Admin", StringComparison.OrdinalIgnoreCase)
            || !await principal.HasPermissionAsync(admin, "action.places.manage", cancellationToken)) return TypedResults.Forbid();
        try
        {
            if (!request.HasFormContentType) return TypedResults.BadRequest(new { message = "Cal enviar multipart/form-data." });
            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            if (file is null) return TypedResults.BadRequest(new { message = "Falta el fitxer JPEG." });
            if (file.Length > 10_000_000) return TypedResults.BadRequest(new { message = "La fotografia supera el límit de 10 MB." });
            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            var result = await service.RegisterUploadAsync(placeId, new PlaceMediaUpload(
                memory.ToArray(), form["source"].ToString(), form["author"].ToString(),
                form["licenseIdentifier"].ToString(), NullIfWhiteSpace(form["licenseUrl"].ToString()),
                form["attribution"].ToString(), form["rightsDeclaration"].ToString(),
                NullIfWhiteSpace(form["evidenceUrl"].ToString())), cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (KeyNotFoundException) { return TypedResults.NotFound(); }
        catch (InvalidOperationException exception) { return TypedResults.Conflict(new { message = exception.Message }); }
    }

    private static async Task<IResult> AcceptAsync(Guid placeId, Guid assetId, AcceptPlaceMediaRequest request,
        ClaimsPrincipal principal, IAdminApplicationService admin, IPlaceMediaApplicationService service,
        CancellationToken cancellationToken)
    {
        var actorId = principal.GetCurrentUserId();
        if (actorId is null || !string.Equals(principal.GetCurrentRoleKey(), "Admin", StringComparison.OrdinalIgnoreCase)
            || !await principal.HasPermissionAsync(admin, "action.places.manage", cancellationToken)) return TypedResults.Forbid();
        try
        {
            await service.AcceptAsync(placeId, assetId, actorId.Value, request.Reason, cancellationToken);
            return TypedResults.Ok(true);
        }
        catch (KeyNotFoundException) { return TypedResults.NotFound(); }
        catch (InvalidOperationException exception) { return TypedResults.Conflict(new { message = exception.Message }); }
    }

    private static Task<IResult> RejectAsync(Guid placeId, Guid assetId, RejectPlaceMediaRequest request, ClaimsPrincipal principal,
        IAdminApplicationService admin, IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAsync(principal, admin, async () => { await service.RejectAsync(placeId, assetId, request.Reason, cancellationToken); return true; }, cancellationToken);

    private static async Task<IResult> ExecuteAsync<T>(ClaimsPrincipal principal, IAdminApplicationService admin,
        Func<Task<T>> action, CancellationToken cancellationToken)
    {
        if (!string.Equals(principal.GetCurrentRoleKey(), "Admin", StringComparison.OrdinalIgnoreCase)
            || !await principal.HasPermissionAsync(admin, "action.places.manage", cancellationToken)) return TypedResults.Forbid();
        try { return TypedResults.Ok(await action()); }
        catch (KeyNotFoundException) { return TypedResults.NotFound(); }
        catch (InvalidOperationException exception) { return TypedResults.Conflict(new { message = exception.Message }); }
    }

    private static string? NullIfWhiteSpace(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
