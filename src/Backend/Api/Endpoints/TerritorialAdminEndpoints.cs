using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.TerritorialImports;

namespace Zuppeto.Api.Endpoints;

internal static class TerritorialAdminEndpoints
{
    public static IEndpointRouteBuilder MapTerritorialAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/territorial").RequireAuthorization();
        group.MapGet("/context", GetContextAsync);
        group.MapPost("/workbooks/inspect", InspectAsync).DisableAntiforgery();
        group.MapGet("/mappings", ListMappingsAsync);
        group.MapGet("/mappings/{id:guid}", GetMappingAsync);
        group.MapPost("/mappings", CreateMappingAsync);
        group.MapPost("/imports", PrepareImportAsync).DisableAntiforgery();
        group.MapGet("/imports", ListImportsAsync);
        group.MapGet("/imports/{id:guid}", GetImportAsync);
        group.MapGet("/imports/{id:guid}/issues", ListIssuesAsync);
        group.MapGet("/imports/{id:guid}/changes", ListChangesAsync);
        group.MapGet("/imports/{id:guid}/preview/source", ListSourcePreviewAsync);
        group.MapGet("/imports/{id:guid}/preview/canonical", ListCanonicalPreviewAsync);
        group.MapPost("/imports/{id:guid}/publish", PublishAsync);
        group.MapPost("/imports/{id:guid}/cancel", CancelAsync);
        group.MapPost("/imports/{id:guid}/revert", RevertAsync);
        group.MapGet("/catalog", ListCatalogAsync);
        group.MapGet("/catalog/{id:guid}", GetCatalogUnitAsync);
        group.MapPost("/catalog/{id:guid}/maintenance", MaintainAsync);
        return app;
    }

    [Authorize]
    private static async Task<IResult> GetContextAsync(ClaimsPrincipal principal, TerritorialAdminService service, CancellationToken ct) =>
        await ExecuteAdmin(principal, actor => service.GetContextAsync(actor, ct));

    [Authorize]
    private static async Task<IResult> InspectAsync(ClaimsPrincipal principal, [FromForm] TerritorialInspectForm form, TerritorialAdminService service, CancellationToken ct)
    {
        if (!TryAdmin(principal, out var actor)) return TypedResults.Forbid();
        if (form.File is null) return TypedResults.BadRequest(new { message = "El fitxer XLSX és obligatori." });
        try
        {
            await using var stream = form.File.OpenReadStream();
            return TypedResults.Ok(await service.InspectAsync(actor, form.DatasetSourceId, form.File.FileName, form.File.Length, stream, ct));
        }
        catch (Exception exception) { return SafeFailure(exception); }
    }

    [Authorize]
    private static async Task<IResult> ListMappingsAsync(ClaimsPrincipal principal, Guid? datasetSourceId, string? schemaFingerprint, TerritorialAdminService service, CancellationToken ct) =>
        await ExecuteAdmin(principal, actor => service.ListMappingsAsync(actor, datasetSourceId, schemaFingerprint, ct));

    [Authorize]
    private static async Task<IResult> GetMappingAsync(ClaimsPrincipal principal, Guid id, TerritorialAdminService service, CancellationToken ct)
    {
        if (!TryAdmin(principal, out var actor)) return TypedResults.Forbid();
        var item = await service.GetMappingAsync(actor, id, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }

    [Authorize]
    private static async Task<IResult> CreateMappingAsync(ClaimsPrincipal principal, CreateTerritorialMappingTemplateRequest request, TerritorialAdminService service, CancellationToken ct) =>
        await ExecuteAdmin(principal, actor => service.CreateMappingAsync(actor, request, ct), created: true);

    [Authorize]
    private static async Task<IResult> PrepareImportAsync(ClaimsPrincipal principal, [FromForm] TerritorialPrepareImportForm form, TerritorialAdminService service, CancellationToken ct)
    {
        if (!TryAdmin(principal, out var actor)) return TypedResults.Forbid();
        if (form.File is null) return TypedResults.BadRequest(new { message = "El fitxer XLSX és obligatori." });
        try
        {
            await using var stream = form.File.OpenReadStream();
            var item = await service.PrepareAsync(actor, form.DatasetSourceId, form.MappingTemplateId, form.DatasetVersion,
                form.File.FileName, form.File.Length, stream, ct);
            return TypedResults.Created($"/api/admin/territorial/imports/{item.Import.Id}", item);
        }
        catch (Exception exception) { return SafeFailure(exception); }
    }

    [Authorize]
    private static async Task<IResult> ListImportsAsync(
        ClaimsPrincipal principal, Guid? countryId, Guid? datasetSourceId, string? status, int page, int pageSize,
        TerritorialAdminService service, CancellationToken ct) =>
        await ExecuteAdmin(principal, actor => service.ListImportsAsync(actor,
            new TerritorialImportQuery(countryId, datasetSourceId, status, page == 0 ? 1 : page, pageSize == 0 ? 25 : pageSize), ct));

    [Authorize]
    private static async Task<IResult> GetImportAsync(ClaimsPrincipal principal, Guid id, TerritorialAdminService service, CancellationToken ct)
    {
        if (!TryAdmin(principal, out var actor)) return TypedResults.Forbid();
        var item = await service.GetImportAsync(actor, id, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }

    [Authorize]
    private static async Task<IResult> ListIssuesAsync(
        ClaimsPrincipal principal, Guid id, string? severity, string? ruleCode, string? sheet, string? field, int page, int pageSize,
        TerritorialAdminService service, CancellationToken ct) =>
        await ExecuteAdmin(principal, actor => service.ListIssuesAsync(actor, id,
            new TerritorialIssueQuery(severity, ruleCode, sheet, field, page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize), ct));

    [Authorize]
    private static async Task<IResult> ListChangesAsync(
        ClaimsPrincipal principal, Guid id, string? kind, string? search, int page, int pageSize,
        TerritorialAdminService service, CancellationToken ct) =>
        await ExecuteAdmin(principal, actor => service.ListChangesAsync(actor, id,
            new TerritorialChangeQuery(kind, search, page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize), ct));

    [Authorize]
    private static async Task<IResult> ListSourcePreviewAsync(
        ClaimsPrincipal principal, Guid id, string? sheet, string? search, int page, int pageSize,
        TerritorialAdminService service, CancellationToken ct) =>
        await ExecuteAdmin(principal, actor => service.ListSourcePreviewAsync(actor, id,
            new TerritorialPreviewQuery(sheet, search, page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize), ct));

    [Authorize]
    private static async Task<IResult> ListCanonicalPreviewAsync(
        ClaimsPrincipal principal, Guid id, string? sheet, string? search, int page, int pageSize,
        TerritorialAdminService service, CancellationToken ct) =>
        await ExecuteAdmin(principal, actor => service.ListCanonicalPreviewAsync(actor, id,
            new TerritorialPreviewQuery(sheet, search, page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize), ct));

    [Authorize]
    private static async Task<IResult> ListCatalogAsync(
        ClaimsPrincipal principal, Guid? countryId, Guid? territorialUnitTypeId, string? status, string? locale,
        Guid? parentId, string? search, bool? selectableLocality, int page, int pageSize,
        TerritorialAdminService service, CancellationToken ct) =>
        await ExecuteAdmin(principal, actor => service.ListCatalogAsync(actor,
            new TerritorialCatalogQuery(countryId, territorialUnitTypeId, status, locale, parentId, search,
                selectableLocality, page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize), ct));

    [Authorize]
    private static async Task<IResult> GetCatalogUnitAsync(
        ClaimsPrincipal principal, Guid id, TerritorialAdminService service, CancellationToken ct)
    {
        if (!TryAdmin(principal, out var actor)) return TypedResults.Forbid();
        var item = await service.GetCatalogUnitAsync(actor, id, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }

    [Authorize]
    private static async Task<IResult> MaintainAsync(
        ClaimsPrincipal principal, Guid id, TerritorialMaintenanceRequest request,
        TerritorialAdminService service, CancellationToken ct) =>
        await ExecuteAdmin(principal, actor => service.MaintainAsync(actor, id, request, ct));

    [Authorize]
    private static async Task<IResult> PublishAsync(ClaimsPrincipal principal, Guid id, TerritorialAdminService service, CancellationToken ct) =>
        await ExecuteAdmin(principal, actor => service.PublishAsync(actor, id, ct));

    [Authorize]
    private static async Task<IResult> CancelAsync(ClaimsPrincipal principal, Guid id, TerritorialAdminService service, CancellationToken ct) =>
        await ExecuteAdmin(principal, actor => service.CancelAsync(actor, id, ct));

    [Authorize]
    private static async Task<IResult> RevertAsync(ClaimsPrincipal principal, Guid id, TerritorialAdminService service, CancellationToken ct) =>
        await ExecuteAdmin(principal, actor => service.RevertAsync(actor, id, ct));

    private static async Task<IResult> ExecuteAdmin<T>(ClaimsPrincipal principal, Func<Guid, Task<T>> action, bool created = false)
    {
        if (!TryAdmin(principal, out var actor)) return TypedResults.Forbid();
        try
        {
            var result = await action(actor);
            return created ? TypedResults.Created(string.Empty, result) : TypedResults.Ok(result);
        }
        catch (Exception exception) { return SafeFailure(exception); }
    }

    private static bool TryAdmin(ClaimsPrincipal principal, out Guid actor)
    {
        actor = principal.GetCurrentUserId() ?? Guid.Empty;
        return actor != Guid.Empty && string.Equals(principal.GetCurrentRoleKey(), "Admin", StringComparison.OrdinalIgnoreCase);
    }

    private static IResult SafeFailure(Exception exception) => exception switch
    {
        UnauthorizedAccessException => TypedResults.Forbid(),
        KeyNotFoundException => TypedResults.NotFound(),
        DbUpdateConcurrencyException => TypedResults.Conflict(new { code = "catalog_version_conflict", message = "El catàleg ha canviat des que es va generar aquest preview." }),
        InvalidDataException => TypedResults.BadRequest(new { message = exception.Message }),
        InvalidOperationException when exception.Message.Contains("publicat", StringComparison.OrdinalIgnoreCase) ||
                                       exception.Message.Contains("obsolet", StringComparison.OrdinalIgnoreCase) ||
                                       exception.Message.Contains("preparada", StringComparison.OrdinalIgnoreCase) ||
                                       exception.Message.Contains("revertir", StringComparison.OrdinalIgnoreCase) =>
            TypedResults.Conflict(new { code = "territorial_import_conflict", message = exception.Message }),
        InvalidOperationException => TypedResults.BadRequest(new { message = exception.Message }),
        _ => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError, title: "No s'ha pogut completar l'operació territorial.")
    };

    internal sealed class TerritorialInspectForm
    {
        public Guid DatasetSourceId { get; init; }
        public IFormFile? File { get; init; }
    }

    internal sealed class TerritorialPrepareImportForm
    {
        public Guid DatasetSourceId { get; init; }
        public Guid MappingTemplateId { get; init; }
        public string DatasetVersion { get; init; } = string.Empty;
        public IFormFile? File { get; init; }
    }
}
