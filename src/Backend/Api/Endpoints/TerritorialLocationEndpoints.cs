using Microsoft.AspNetCore.Mvc;
using Zuppeto.Application.TerritorialImports;

namespace Zuppeto.Api.Endpoints;

internal static class TerritorialLocationEndpoints
{
    public static IEndpointRouteBuilder MapTerritorialLocationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/territorial");
        group.MapGet("/countries", async (TerritorialLocationService service, CancellationToken ct) =>
            TypedResults.Ok(await service.ListCountriesAsync(ct)));
        group.MapGet("/localities", SearchAsync);
        group.MapPost("/location/validate", ValidateAsync);
        return app;
    }

    private static async Task<IResult> SearchAsync(
        Guid countryId, string? search, int? page, int? pageSize, TerritorialLocationService service, CancellationToken ct)
    {
        try { return TypedResults.Ok(await service.SearchLocalitiesAsync(countryId, search, page is null or 0 ? 1 : page.Value, pageSize is null or 0 ? 20 : pageSize.Value, ct)); }
        catch (KeyNotFoundException) { return TypedResults.NotFound(); }
        catch (InvalidOperationException exception) { return TypedResults.BadRequest(new { message = exception.Message }); }
    }

    private static async Task<IResult> ValidateAsync(
        [FromBody] TerritorialLocationValidationRequest request, TerritorialLocationService service, CancellationToken ct)
    {
        try { await service.ValidateSelectionAsync(request, ct); return TypedResults.NoContent(); }
        catch (KeyNotFoundException) { return TypedResults.NotFound(); }
        catch (InvalidOperationException exception) { return TypedResults.BadRequest(new { message = exception.Message }); }
    }
}
