using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Zuppeto.Application.Admin;
using Zuppeto.Application.Results;
using Zuppeto.Application.Validation;
using Zuppeto.Api.Validation;

namespace Zuppeto.Api.Endpoints;

internal static class GeographicAdminEndpoints
{
    public static IEndpointRouteBuilder MapGeographicAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin").RequireAuthorization().WithTags("Geographic admin");

        group.MapGet("/countries", ListCountriesAsync);
        group.MapGet("/countries/{id:guid}", GetCountryAsync);
        group.MapPost("/countries", CreateCountryAsync);
        group.MapPut("/countries/{id:guid}", UpdateCountryAsync);
        group.MapDelete("/countries/{id:guid}", DeleteCountryAsync);

        return app;
    }

    private const string GeographicManagePermission = "action.geographic.manage";

    [Authorize]
    private static async Task<Results<Ok<IReadOnlyList<CountryAdminDto>>, ForbidHttpResult>> ListCountriesAsync(
        ClaimsPrincipal principal,
        IAdminApplicationService adminService,
        IGeographicAdminAppService geographicService,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(adminService, GeographicManagePermission, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(await geographicService.ListCountriesAsync(cancellationToken));
    }

    [Authorize]
    private static async Task<Results<Ok<CountryAdminDto>, NotFound, ForbidHttpResult>> GetCountryAsync(
        ClaimsPrincipal principal,
        Guid id,
        IAdminApplicationService adminService,
        IGeographicAdminAppService geographicService,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(adminService, GeographicManagePermission, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var country = await geographicService.GetCountryAsync(id, cancellationToken);
        return country is null ? TypedResults.NotFound() : TypedResults.Ok(country);
    }

    [Authorize]
    private static async Task<Results<Created<CountryAdminDto>, Conflict<string>, ForbidHttpResult, ValidationProblem>> CreateCountryAsync(
        ClaimsPrincipal principal,
        CreateCountryRequest request,
        IValidator<CreateCountryRequest> validator,
        IAdminApplicationService adminService,
        IGeographicAdminAppService geographicService,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(adminService, GeographicManagePermission, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var validation = validator.Validate(request);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await geographicService.CreateCountryAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.Conflict(result.Failure?.Message ?? "No s’ha pogut crear el país.");
        }

        var created = result.Value!;
        return TypedResults.Created($"/api/admin/countries/{created.Id}", created);
    }

    [Authorize]
    private static async Task<Results<Ok<CountryAdminDto>, NotFound, Conflict<string>, ForbidHttpResult, ValidationProblem>> UpdateCountryAsync(
        ClaimsPrincipal principal,
        Guid id,
        UpdateCountryRequest request,
        IValidator<UpdateCountryRequest> validator,
        IAdminApplicationService adminService,
        IGeographicAdminAppService geographicService,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(adminService, GeographicManagePermission, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var validation = validator.Validate(request);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await geographicService.UpdateCountryAsync(id, request, cancellationToken);
        if (!result.IsSuccess && result.Failure?.Kind == FailureKind.NotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.Conflict(result.Failure?.Message ?? "No s’ha pogut actualitzar el país.");
        }

        return TypedResults.Ok(result.Value!);
    }

    [Authorize]
    private static async Task<Results<NoContent, NotFound, Conflict<string>, ForbidHttpResult>> DeleteCountryAsync(
        ClaimsPrincipal principal,
        Guid id,
        IAdminApplicationService adminService,
        IGeographicAdminAppService geographicService,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(adminService, GeographicManagePermission, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var result = await geographicService.DeleteCountryAsync(id, cancellationToken);
        if (!result.IsSuccess && result.Failure?.Kind == FailureKind.NotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.Conflict(result.Failure?.Message ?? "No s’ha pogut esborrar el país.");
        }

        return TypedResults.NoContent();
    }

}
