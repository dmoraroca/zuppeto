using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Admin;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Common;

namespace Zuppeto.Api.Endpoints;

internal static class PlaceCatalogGovernanceEndpoints
{
    public static IEndpointRouteBuilder MapPlaceCatalogGovernanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/place-catalog-governance").RequireAuthorization().WithTags("Place catalog governance");
        group.MapGet("/", (ClaimsPrincipal p, IAdminApplicationService a, IPlaceCatalogGovernanceService s, CancellationToken ct) => Execute(p, a, _ => s.GetAsync(ct), ct));
        group.MapGet("/places/{id:guid}/traceability", (Guid id, ClaimsPrincipal p, IAdminApplicationService a, IPlaceCatalogGovernanceService s, CancellationToken ct) =>
            Execute(p, a, _ => s.GetTraceabilityAsync(id, ct), ct));
        group.MapPost("/matches/{id:guid}/resolve", (Guid id, ResolvePlaceMatchRequest r, ClaimsPrincipal p, IAdminApplicationService a, IPlaceCatalogGovernanceService s, CancellationToken ct) => Execute(p, a, async actor => { await s.ResolveMatchAsync(id, r, actor, ct); return true; }, ct));
        group.MapPost("/conflicts/{id:guid}/resolve", (Guid id, ResolvePlaceConflictRequest r, ClaimsPrincipal p, IAdminApplicationService a, IPlaceCatalogGovernanceService s, CancellationToken ct) => Execute(p, a, async actor => { await s.ResolveConflictAsync(id, r, actor, ct); return true; }, ct));
        group.MapPost("/places/{id:guid}/lifecycle", (Guid id, ChangePlaceLifecycleRequest r, ClaimsPrincipal p, IAdminApplicationService a, IPlaceCatalogGovernanceService s, CancellationToken ct) => Execute(p, a, async actor => { await s.ChangeLifecycleAsync(id, r, actor, ct); return true; }, ct));
        group.MapPost("/sources", (RegisterOfficialDatasetRequest r, ClaimsPrincipal p, IAdminApplicationService a, IPlaceCatalogGovernanceService s, CancellationToken ct) => Execute(p, a, actor => s.RegisterDatasetAsync(r, actor, ct), ct, true));
        group.MapPost("/sources/{id:guid}/review", (Guid id, ReviewOfficialDatasetRequest r, ClaimsPrincipal p, IAdminApplicationService a, IPlaceCatalogGovernanceService s, CancellationToken ct) => Execute(p, a, async actor => { await s.ReviewDatasetAsync(id, r, actor, ct); return true; }, ct));
        return app;
    }

    private static async Task<IResult> Execute<T>(ClaimsPrincipal principal, IAdminApplicationService admin,
        Func<Guid, Task<T>> action, CancellationToken ct, bool created = false)
    {
        var actor = principal.GetCurrentUserId();
        var allowed = actor.HasValue && string.Equals(principal.GetCurrentRoleKey(), "Admin", StringComparison.OrdinalIgnoreCase)
            && await principal.HasPermissionAsync(admin, "action.places.manage", ct);
        if (!allowed) return TypedResults.Forbid();
        try
        {
            var value = await action(actor!.Value);
            return created ? TypedResults.Created(string.Empty, value) : TypedResults.Ok(value);
        }
        catch (KeyNotFoundException) { return TypedResults.NotFound(); }
        catch (DomainRuleException exception) { return TypedResults.BadRequest(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return TypedResults.Conflict(new { message = exception.Message }); }
        catch (DbUpdateException) { return TypedResults.Conflict(new { message = "La decisió entra en conflicte amb l’estat actual." }); }
    }
}
