using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using YepPet.Application.Admin;
using YepPet.Application.Places;
using YepPet.Application.Validation;
using YepPet.Api.Validation;

namespace YepPet.Api.Endpoints;

internal static class PlaceEndpoints
{
    public static IEndpointRouteBuilder MapPlaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/places").WithTags("Places");
        var adminGroup = app.MapGroup("/api/admin/places").RequireAuthorization().WithTags("Places");

        group.MapGet("/", SearchAsync);
        group.MapGet("/searches/recent", GetRecentSearchesAsync);
        group.MapGet("/external/search", SearchExternalPlacesPreviewAsync);
        group.MapGet("/cities/search", SearchAvailableCitiesAsync)
            .WithName("SearchAvailablePlaceCities")
            .WithSummary("Search distinct cities that have at least one place (typeahead).")
            .WithDescription(
                "Returns city names (distinct) whose places match the substring q (case-insensitive). " +
                "Requires at least 3 valid characters in q after normalization. " +
                "Optional limit: default 50, maximum 100.");

        group.MapGet("/cities", GetAvailableCitiesAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapPost("/", SaveAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        adminGroup.MapGet("/", AdminSearchAsync);
        adminGroup.MapGet("/{id:guid}", AdminGetByIdAsync);
        adminGroup.MapPost("/", AdminSaveAsync);
        adminGroup.MapPut("/{id:guid}", AdminUpdateAsync);
        adminGroup.MapDelete("/{id:guid}", AdminDeleteAsync);

        return app;
    }

    private static async Task<Ok<IReadOnlyCollection<PlaceSummaryDto>>> SearchAsync(
        [AsParameters] PlaceSearchQuery query,
        IPlaceApplicationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.SearchAsync(
            new PlaceSearchRequest(query.SearchText, query.City, query.Type, query.PetCategory ?? "All"),
            cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<Ok<IReadOnlyCollection<string>>> GetAvailableCitiesAsync(
        IPlaceApplicationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAvailableCitiesAsync(cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<IReadOnlyCollection<PlaceSearchHistoryDto>>> GetRecentSearchesAsync(
        int? limit,
        IPlaceApplicationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetRecentSearchesAsync(limit ?? 20, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<IReadOnlyCollection<PlaceExternalCandidateDto>>> SearchExternalPlacesPreviewAsync(
        [AsParameters] PlaceExternalSearchQuery query,
        IPlaceApplicationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.SearchExternalPreviewAsync(
            new PlaceExternalSearchRequest(query.Query, query.City, query.Type, query.Limit),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<IReadOnlyCollection<PlaceCitySuggestionDto>>, ValidationProblem>> SearchAvailableCitiesAsync(
        [AsParameters] PlaceCitySearchRequest query,
        IValidator<PlaceCitySearchRequest> validator,
        IPlaceApplicationService service,
        CancellationToken cancellationToken)
    {
        var validation = validator.Validate(query);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await service.SearchAvailableCitiesAsync(query, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<PlaceDetailDto>, NotFound>> GetByIdAsync(
        Guid id,
        IPlaceApplicationService service,
        CancellationToken cancellationToken)
    {
        var place = await service.GetByIdAsync(id, cancellationToken);
        return place is null ? TypedResults.NotFound() : TypedResults.Ok(place);
    }

    private static async Task<Results<Created<Guid>, ValidationProblem>> SaveAsync(
        PlaceUpsertRequest request,
        IValidator<PlaceUpsertRequest> validator,
        IPlaceApplicationService service,
        CancellationToken cancellationToken)
    {
        var validation = validator.Validate(request);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var id = await service.SaveAsync(request, cancellationToken);
        return TypedResults.Created($"/api/places/{id}", id);
    }

    private static async Task<Results<Ok<Guid>, ValidationProblem>> UpdateAsync(
        Guid id,
        PlaceUpsertRequest request,
        IValidator<PlaceUpsertRequest> validator,
        IPlaceApplicationService service,
        CancellationToken cancellationToken)
    {
        var normalized = request with { Id = id };
        var validation = validator.Validate(normalized);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var resultId = await service.SaveAsync(normalized, cancellationToken);
        return TypedResults.Ok(resultId);
    }

    [Authorize]
    private static async Task<Results<Ok<IReadOnlyCollection<PlaceSummaryDto>>, ForbidHttpResult>> AdminSearchAsync(
        [AsParameters] PlaceSearchQuery query,
        ClaimsPrincipal principal,
        IAdminApplicationService adminService,
        IPlaceApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(adminService, "action.places.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var result = await service.SearchAsync(
            new PlaceSearchRequest(query.SearchText, query.City, query.Type, query.PetCategory ?? "All"),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    [Authorize]
    private static async Task<Results<Ok<PlaceDetailDto>, NotFound, ForbidHttpResult>> AdminGetByIdAsync(
        Guid id,
        ClaimsPrincipal principal,
        IAdminApplicationService adminService,
        IPlaceApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(adminService, "action.places.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var place = await service.GetByIdAsync(id, cancellationToken);
        return place is null ? TypedResults.NotFound() : TypedResults.Ok(place);
    }

    [Authorize]
    private static async Task<Results<Created<Guid>, ForbidHttpResult, ValidationProblem>> AdminSaveAsync(
        PlaceUpsertRequest request,
        ClaimsPrincipal principal,
        IAdminApplicationService adminService,
        IValidator<PlaceUpsertRequest> validator,
        IPlaceApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(adminService, "action.places.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var validation = validator.Validate(request);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var id = await service.SaveAsync(request, cancellationToken);
        return TypedResults.Created($"/api/admin/places/{id}", id);
    }

    [Authorize]
    private static async Task<Results<Ok<Guid>, ForbidHttpResult, ValidationProblem>> AdminUpdateAsync(
        Guid id,
        PlaceUpsertRequest request,
        ClaimsPrincipal principal,
        IAdminApplicationService adminService,
        IValidator<PlaceUpsertRequest> validator,
        IPlaceApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(adminService, "action.places.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var normalized = request with { Id = id };
        var validation = validator.Validate(normalized);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var resultId = await service.SaveAsync(normalized, cancellationToken);
        return TypedResults.Ok(resultId);
    }

    [Authorize]
    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> AdminDeleteAsync(
        Guid id,
        ClaimsPrincipal principal,
        IAdminApplicationService adminService,
        IPlaceApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!await principal.HasPermissionAsync(adminService, "action.places.manage", cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var deleted = await service.DeleteAsync(id, cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    internal sealed record PlaceSearchQuery(
        string? SearchText,
        string? City,
        string? Type,
        string? PetCategory);

    internal sealed record PlaceExternalSearchQuery(
        string? Query,
        string? City,
        string? Type,
        int? Limit);
}
