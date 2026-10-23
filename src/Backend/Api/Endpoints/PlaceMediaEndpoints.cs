using System.Security.Claims;
using Zuppeto.Application.Admin;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Places;

namespace Zuppeto.Api.Endpoints;

internal static class PlaceMediaEndpoints
{
    public static IEndpointRouteBuilder MapPlaceMediaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/place-media/pending", ListPendingGlobalAsync)
            .RequireAuthorization().WithTags("Place media administration");
        app.MapGet("/api/admin/place-media/reports", ListReportsAsync)
            .RequireAuthorization().WithTags("Place media administration");
        app.MapPost("/api/admin/place-media/reports/{reportId:guid}/resolve", ResolveReportAsync)
            .RequireAuthorization().WithTags("Place media administration");
        var media = app.MapGroup("/api/places/{placeId:guid}/media").WithTags("Place media");
        media.MapGet("/", ListPublicAsync).AllowAnonymous();
        media.MapGet("/{assetId:guid}/{variant}", GetBlobAsync).AllowAnonymous();
        media.MapPost("/upload", UploadUserMediaAsync).RequireAuthorization().DisableAntiforgery();
        media.MapPost("/{assetId:guid}/reports", ReportAsync).RequireAuthorization();

        var admin = app.MapGroup("/api/admin/places/{placeId:guid}/media")
            .RequireAuthorization().WithTags("Place media administration");
        admin.MapGet("/", ListAsync);
        admin.MapGet("/pending", ListPendingAsync);
        admin.MapGet("/history", ListHistoryAsync);
        admin.MapGet("/{assetId:guid}/blob/{variant}", GetAdminBlobAsync);
        admin.MapPost("/wikimedia/import", ImportWikimediaAsync);
        admin.MapGet("/mapillary/candidates", SearchMapillaryAsync);
        admin.MapPost("/upload", UploadAdminMediaAsync).DisableAntiforgery();
        admin.MapPost("/{assetId:guid}/accept", AcceptAsync);
        admin.MapPost("/{assetId:guid}/primary", SetPrimaryAsync);
        admin.MapPost("/{assetId:guid}/reject", RejectAsync);
        admin.MapPost("/{assetId:guid}/quarantine", QuarantineAsync);
        admin.MapPost("/{assetId:guid}/withdraw", WithdrawAsync);
        return app;
    }

    private static async Task<IResult> ListPublicAsync(Guid placeId, int? skip, int? take,
        IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListPublicAsync(placeId, skip ?? 0, take ?? 20, cancellationToken));

    private static Task<IResult> ListPendingGlobalAsync(int? skip, int? take, ClaimsPrincipal principal,
        IAdminApplicationService admin, IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAdminAsync(principal, admin, () => service.ListPendingAsync(skip ?? 0, take ?? 50, cancellationToken), cancellationToken);

    private static Task<IResult> ListReportsAsync(int? skip, int? take, ClaimsPrincipal principal,
        IAdminApplicationService admin, IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAdminAsync(principal, admin, () => service.ListPendingReportsAsync(skip ?? 0, take ?? 50, cancellationToken), cancellationToken);

    private static async Task<IResult> ResolveReportAsync(Guid reportId, ResolvePlaceMediaReportRequest request,
        ClaimsPrincipal principal, IAdminApplicationService admin, IPlaceMediaApplicationService service,
        CancellationToken cancellationToken)
    {
        var actor = principal.GetCurrentUserId();
        if (actor is null || !await IsAdminAsync(principal, admin, cancellationToken)) return TypedResults.Forbid();
        try { await service.ResolveReportAsync(reportId, actor.Value, request.Decision, request.Reason, cancellationToken); return TypedResults.Ok(); }
        catch (KeyNotFoundException) { return TypedResults.NotFound(); }
        catch (InvalidOperationException exception) { return TypedResults.BadRequest(new { message = exception.Message }); }
    }

    private static async Task<IResult> GetBlobAsync(Guid placeId, Guid assetId, string variant,
        HttpContext context, IPlaceMediaApplicationService service, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PlaceMediaVariant>(variant, true, out var parsed)) return TypedResults.NotFound();
        var blob = await service.GetBlobAsync(placeId, assetId, parsed, false, cancellationToken);
        if (blob is null) return TypedResults.NotFound();
        var etag = $"\"{blob.Sha256}\"";
        if (context.Request.Headers.IfNoneMatch.Contains(etag)) return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        context.Response.Headers.ETag = etag;
        context.Response.Headers.CacheControl = "public,max-age=86400,immutable";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        return Results.Bytes(blob.Content, blob.MimeType, lastModified: blob.CreatedAtUtc);
    }

    private static async Task<IResult> UploadUserMediaAsync(Guid placeId, HttpRequest request,
        ClaimsPrincipal principal, IPlaceMediaApplicationService service, CancellationToken cancellationToken)
    {
        var userId = principal.GetCurrentUserId();
        if (userId is null) return TypedResults.Unauthorized();
        try
        {
            if (!request.HasFormContentType) return BadRequest("Cal enviar multipart/form-data.");
            var form = await request.ReadFormAsync(cancellationToken);
            var files = form.Files.Where(file => file.Name is "files" or "file").ToArray();
            if (files.Length is < 1 or > 5) return BadRequest("Selecciona entre 1 i 5 fotografies.");
            var rights = form["rightsDeclaration"].ToString().Trim();
            if (!string.Equals(form["rightsAccepted"], "true", StringComparison.OrdinalIgnoreCase) || rights.Length < 20)
                return BadRequest("Cal acceptar i conservar la declaració provisional de drets.");

            var uploaded = new List<PlaceMediaAssetDto>(files.Length);
            foreach (var file in files)
            {
                if (file.Length is <= 0 or > 10_000_000) return BadRequest("Cada fotografia ha de tenir un màxim de 10 MB.");
                await using var stream = file.OpenReadStream();
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory, cancellationToken);
                uploaded.Add(await service.RegisterUploadAsync(placeId, new PlaceMediaUpload(
                    memory.ToArray(), file.FileName, file.ContentType, nameof(PlaceMediaSource.User),
                    principal.Identity?.Name ?? "Usuari PetiLoc", "PETILOC-USER-RIGHTS", null,
                    "Aportació d’un usuari de PetiLoc", rights, null, userId), cancellationToken));
            }
            return TypedResults.Ok(uploaded);
        }
        catch (KeyNotFoundException) { return TypedResults.NotFound(); }
        catch (InvalidOperationException exception) { return TypedResults.Conflict(new { message = exception.Message }); }
    }

    private static async Task<IResult> ReportAsync(Guid placeId, Guid assetId, PlaceMediaReportRequest request,
        ClaimsPrincipal principal, IPlaceMediaApplicationService service, CancellationToken cancellationToken)
    {
        var userId = principal.GetCurrentUserId();
        if (userId is null) return TypedResults.Unauthorized();
        try { await service.ReportAsync(placeId, assetId, userId.Value, request.Reason, cancellationToken); return TypedResults.Ok(); }
        catch (KeyNotFoundException) { return TypedResults.NotFound(); }
        catch (InvalidOperationException exception) { return TypedResults.BadRequest(new { message = exception.Message }); }
    }

    private static Task<IResult> ListAsync(Guid placeId, ClaimsPrincipal principal, IAdminApplicationService admin,
        IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAdminAsync(principal, admin, () => service.ListAsync(placeId, cancellationToken), cancellationToken);

    private static Task<IResult> ListPendingAsync(Guid placeId, int? skip, int? take, ClaimsPrincipal principal,
        IAdminApplicationService admin, IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAdminAsync(principal, admin, () => service.ListPendingAsync(skip ?? 0, take ?? 50, cancellationToken), cancellationToken);

    private static Task<IResult> ListHistoryAsync(Guid placeId, ClaimsPrincipal principal,
        IAdminApplicationService admin, IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAdminAsync(principal, admin, () => service.ListModerationHistoryAsync(placeId, cancellationToken), cancellationToken);

    private static async Task<IResult> GetAdminBlobAsync(Guid placeId, Guid assetId, string variant,
        ClaimsPrincipal principal, IAdminApplicationService admin, IPlaceMediaApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync(principal, admin, cancellationToken)
            || !Enum.TryParse<PlaceMediaVariant>(variant, true, out var parsed)) return TypedResults.Forbid();
        var blob = await service.GetBlobAsync(placeId, assetId, parsed, true, cancellationToken);
        return blob is null ? TypedResults.NotFound() : Results.Bytes(blob.Content, blob.MimeType);
    }

    private static Task<IResult> ImportWikimediaAsync(Guid placeId, ClaimsPrincipal principal, IAdminApplicationService admin,
        IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAdminAsync(principal, admin, () => service.ImportFromWikimediaAsync(placeId, cancellationToken), cancellationToken);

    private static Task<IResult> SearchMapillaryAsync(Guid placeId, ClaimsPrincipal principal, IAdminApplicationService admin,
        IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAdminAsync(principal, admin, () => service.SearchMapillaryAsync(placeId, cancellationToken), cancellationToken);

    private static Task<IResult> SetPrimaryAsync(Guid placeId, Guid assetId, ClaimsPrincipal principal, IAdminApplicationService admin,
        IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAdminAsync(principal, admin, async () => { await service.SetPrimaryAsync(placeId, assetId, cancellationToken); return true; }, cancellationToken);

    private static async Task<IResult> UploadAdminMediaAsync(Guid placeId, HttpRequest request, ClaimsPrincipal principal,
        IAdminApplicationService admin, IPlaceMediaApplicationService service, CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync(principal, admin, cancellationToken)) return TypedResults.Forbid();
        try
        {
            if (!request.HasFormContentType) return BadRequest("Cal enviar multipart/form-data.");
            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            if (file is null) return BadRequest("Falta el fitxer d’imatge.");
            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            return TypedResults.Ok(await service.RegisterUploadAsync(placeId, new PlaceMediaUpload(
                memory.ToArray(), file.FileName, file.ContentType, form["source"].ToString(), form["author"].ToString(),
                form["licenseIdentifier"].ToString(), NullIfWhiteSpace(form["licenseUrl"]), form["attribution"].ToString(),
                form["rightsDeclaration"].ToString(), NullIfWhiteSpace(form["evidenceUrl"]), principal.GetCurrentUserId()), cancellationToken));
        }
        catch (KeyNotFoundException) { return TypedResults.NotFound(); }
        catch (InvalidOperationException exception) { return TypedResults.Conflict(new { message = exception.Message }); }
    }

    private static async Task<IResult> AcceptAsync(Guid placeId, Guid assetId, AcceptPlaceMediaRequest request,
        ClaimsPrincipal principal, IAdminApplicationService admin, IPlaceMediaApplicationService service, CancellationToken cancellationToken)
    {
        var actorId = principal.GetCurrentUserId();
        if (actorId is null || !await IsAdminAsync(principal, admin, cancellationToken)) return TypedResults.Forbid();
        try { await service.AcceptAsync(placeId, assetId, actorId.Value, request.Reason, cancellationToken); return TypedResults.Ok(); }
        catch (KeyNotFoundException) { return TypedResults.NotFound(); }
        catch (InvalidOperationException exception) { return TypedResults.Conflict(new { message = exception.Message }); }
    }

    private static Task<IResult> RejectAsync(Guid placeId, Guid assetId, RejectPlaceMediaRequest request, ClaimsPrincipal principal,
        IAdminApplicationService admin, IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAdminWithActorAsync(placeId, assetId, request.Reason, principal, admin, service.RejectAsync, cancellationToken);

    private static Task<IResult> QuarantineAsync(Guid placeId, Guid assetId, RejectPlaceMediaRequest request, ClaimsPrincipal principal,
        IAdminApplicationService admin, IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAdminWithActorAsync(placeId, assetId, request.Reason, principal, admin, service.QuarantineAsync, cancellationToken);

    private static Task<IResult> WithdrawAsync(Guid placeId, Guid assetId, RejectPlaceMediaRequest request, ClaimsPrincipal principal,
        IAdminApplicationService admin, IPlaceMediaApplicationService service, CancellationToken cancellationToken) =>
        ExecuteAdminWithActorAsync(placeId, assetId, request.Reason, principal, admin, service.WithdrawAsync, cancellationToken);

    private static async Task<IResult> ExecuteAdminWithActorAsync(Guid placeId, Guid assetId, string reason,
        ClaimsPrincipal principal, IAdminApplicationService admin,
        Func<Guid, Guid, Guid, string, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        var actor = principal.GetCurrentUserId();
        if (actor is null || !await IsAdminAsync(principal, admin, cancellationToken)) return TypedResults.Forbid();
        try { await action(placeId, assetId, actor.Value, reason, cancellationToken); return TypedResults.Ok(); }
        catch (KeyNotFoundException) { return TypedResults.NotFound(); }
        catch (InvalidOperationException exception) { return TypedResults.Conflict(new { message = exception.Message }); }
    }

    private static async Task<IResult> ExecuteAdminAsync<T>(ClaimsPrincipal principal, IAdminApplicationService admin,
        Func<Task<T>> action, CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync(principal, admin, cancellationToken)) return TypedResults.Forbid();
        try { return TypedResults.Ok(await action()); }
        catch (KeyNotFoundException) { return TypedResults.NotFound(); }
        catch (InvalidOperationException exception) { return TypedResults.Conflict(new { message = exception.Message }); }
    }

    private static Task<bool> IsAdminAsync(ClaimsPrincipal principal, IAdminApplicationService admin, CancellationToken cancellationToken) =>
        !string.Equals(principal.GetCurrentRoleKey(), "Admin", StringComparison.OrdinalIgnoreCase)
            ? Task.FromResult(false)
            : principal.HasPermissionAsync(admin, "action.places.manage", cancellationToken);

    private static IResult BadRequest(string message) => TypedResults.BadRequest(new { message });
    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
