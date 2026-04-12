using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using YepPet.Application.Admin;
using YepPet.Application.Results;
using YepPet.Application.Validation;
using YepPet.Api.Validation;

namespace YepPet.Api.Endpoints;

internal static class GeographicAdminEndpoints
{
    public static IEndpointRouteBuilder MapGeographicAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin").RequireAuthorization();

        group.MapGet("/countries", ListCountriesAsync);
        group.MapGet("/countries/{id:guid}", GetCountryAsync);
        group.MapPost("/countries", CreateCountryAsync);
        group.MapPut("/countries/{id:guid}", UpdateCountryAsync);
        group.MapDelete("/countries/{id:guid}", DeleteCountryAsync);

        group.MapGet("/cities", ListCitiesAsync);
        group.MapGet("/cities/{id:guid}", GetCityAsync);
        group.MapPost("/cities", CreateCityAsync);
        group.MapPut("/cities/{id:guid}", UpdateCityAsync);
        group.MapDelete("/cities/{id:guid}", DeleteCityAsync);

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
            return TypedResults.Conflict(result.Failure?.Message ?? "Unable to create country.");
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
            return TypedResults.Conflict(result.Failure?.Message ?? "Unable to update country.");
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
            return TypedResults.Conflict(result.Failure?.Message ?? "Unable to delete country.");
        }

        return TypedResults.NoContent();
    }

    [Authorize]
    private static async Task<Results<Ok<IReadOnlyList<CityAdminDto>>, ForbidHttpResult>> ListCitiesAsync(
        ClaimsPrincipal principal,
        Guid? countryId,
        IAdminApplicationService adminService,
        IGeographicAdminAppService geographicService,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(adminService, GeographicManagePermission, cancellationToken))
        {
            return TypedResults.Forbid();
        }

        return TypedResults.Ok(await geographicService.ListCitiesAsync(countryId, cancellationToken));
    }

    [Authorize]
    private static async Task<Results<Ok<CityAdminDto>, NotFound, ForbidHttpResult>> GetCityAsync(
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

        var city = await geographicService.GetCityAsync(id, cancellationToken);
        return city is null ? TypedResults.NotFound() : TypedResults.Ok(city);
    }

    [Authorize]
    private static async Task<Results<Created<CityAdminDto>, Conflict<string>, NotFound, ForbidHttpResult, ValidationProblem>> CreateCityAsync(
        ClaimsPrincipal principal,
        CreateCityRequest request,
        IValidator<CreateCityRequest> validator,
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

        var result = await geographicService.CreateCityAsync(request, cancellationToken);
        if (!result.IsSuccess && result.Failure?.Kind == FailureKind.NotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.Conflict(result.Failure?.Message ?? "Unable to create city.");
        }

        var created = result.Value!;
        return TypedResults.Created($"/api/admin/cities/{created.Id}", created);
    }

    [Authorize]
    private static async Task<Results<Ok<CityAdminDto>, NotFound, Conflict<string>, ForbidHttpResult, ValidationProblem>> UpdateCityAsync(
        ClaimsPrincipal principal,
        Guid id,
        UpdateCityRequest request,
        IValidator<UpdateCityRequest> validator,
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

        var result = await geographicService.UpdateCityAsync(id, request, cancellationToken);
        if (!result.IsSuccess && result.Failure?.Kind == FailureKind.NotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.Conflict(result.Failure?.Message ?? "Unable to update city.");
        }

        return TypedResults.Ok(result.Value!);
    }

    [Authorize]
    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> DeleteCityAsync(
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

        var result = await geographicService.DeleteCityAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
