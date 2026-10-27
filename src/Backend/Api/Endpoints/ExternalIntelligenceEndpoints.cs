using System.Security.Claims;
using System.Text;
using Zuppeto.Application.Admin;
using Zuppeto.Application.ExternalIntelligence;

namespace Zuppeto.Api.Endpoints;

internal static class ExternalIntelligenceEndpoints
{
    public static IEndpointRouteBuilder MapExternalIntelligenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/external-intelligence").RequireAuthorization().WithTags("External intelligence");
        group.MapGet("/requests", (ClaimsPrincipal p, IAdminApplicationService a, IExternalIntelligenceService s,
            DateTimeOffset? fromUtc, DateTimeOffset? toUtc, string? provider, string? model, Guid? municipalityId,
            ExternalRequestStatus? status, decimal? minimumCostUsd, int page, int pageSize, CancellationToken ct) =>
            Execute(p, a, _ => s.ListAsync(new(fromUtc, toUtc, provider, model, municipalityId, status, minimumCostUsd,
                page <= 0 ? 1 : page, pageSize <= 0 ? 25 : pageSize), ct), ct));
        group.MapGet("/requests/{id:guid}", async (Guid id, ClaimsPrincipal p, IAdminApplicationService a, IExternalIntelligenceService s, CancellationToken ct) =>
        {
            var result = await Execute(p, a, _ => s.GetAsync(id, ct), ct);
            return result;
        });
        group.MapGet("/billing", (ClaimsPrincipal p, IAdminApplicationService a, IExternalIntelligenceService s, CancellationToken ct) =>
            Execute(p, a, _ => s.GetBillingAsync(ct), ct));
        group.MapGet("/budget", (ClaimsPrincipal p, IAdminApplicationService a, IExternalIntelligenceService s, CancellationToken ct) =>
            Execute(p, a, _ => s.GetBudgetAsync(ct), ct));
        group.MapPut("/budget", (ExternalBudgetSettingsDto request, ClaimsPrincipal p, IAdminApplicationService a, IExternalIntelligenceService s, CancellationToken ct) =>
            Execute(p, a, actor => s.UpdateBudgetAsync(request, actor, ct), ct));
        group.MapPost("/simulate", (ExternalCostSimulationRequest request, ClaimsPrincipal p, IAdminApplicationService a, IExternalIntelligenceService s, CancellationToken ct) =>
            Execute(p, a, _ => s.SimulateAsync(request, ct), ct));
        group.MapPost("/retention/apply", (ClaimsPrincipal p, IAdminApplicationService a, IExternalIntelligenceService s, CancellationToken ct) =>
            Execute(p, a, _ => s.ApplyRetentionAsync(ct), ct));
        group.MapPost("/execute", (ExternalIntelligenceQuery request, ClaimsPrincipal p, IAdminApplicationService a, IExternalIntelligenceService s, CancellationToken ct) =>
            Execute(p, a, actor => s.ExecuteAsync(request, new(Guid.NewGuid(), actor, null, Estimate(request)), ct), ct));
        group.MapGet("/requests.csv", async (ClaimsPrincipal p, IAdminApplicationService a, IExternalIntelligenceService s, CancellationToken ct) =>
        {
            var actor = await Authorize(p, a, ct);
            if (!actor.HasValue) return Results.Forbid();
            var data = await s.ListAsync(new(Page: 1, PageSize: 100), ct);
            var csv = new StringBuilder("id,provider,api,model,status,created_at_utc,estimated_usd,actual_usd,reused\n");
            foreach (var x in data.Items)
                csv.AppendLine(string.Join(',', x.Id, Csv(x.Provider), Csv(x.Api), Csv(x.Model), x.Status, x.CreatedAtUtc.ToString("O"),
                    x.EstimatedCostUsd.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    x.ActualCostUsd?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty, x.Reused));
            return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "petiloc-external-requests.csv");
        });
        return app;
    }

    private static async Task<IResult> Execute<T>(ClaimsPrincipal principal, IAdminApplicationService admin,
        Func<Guid, Task<T>> action, CancellationToken cancellationToken)
    {
        var actor = await Authorize(principal, admin, cancellationToken);
        if (!actor.HasValue) return TypedResults.Forbid();
        try { return TypedResults.Ok(await action(actor.Value)); }
        catch (ArgumentException exception) { return TypedResults.BadRequest(new { message = exception.Message }); }
        catch (ExternalProviderException exception)
        {
            return Results.Problem(statusCode: exception.HttpStatus is >= 400 and <= 599 ? exception.HttpStatus : 503,
                title: "No s'ha pogut completar la consulta externa.", extensions: new Dictionary<string, object?> { ["code"] = exception.ErrorCode });
        }
    }

    private static async Task<Guid?> Authorize(ClaimsPrincipal principal, IAdminApplicationService admin, CancellationToken ct)
    {
        var actor = principal.GetCurrentUserId();
        return actor.HasValue && string.Equals(principal.GetCurrentRoleKey(), "Admin", StringComparison.OrdinalIgnoreCase)
            && await principal.HasPermissionAsync(admin, "action.places.manage", ct) ? actor : null;
    }

    private static decimal Estimate(ExternalIntelligenceQuery request) => 0.05m;
    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
