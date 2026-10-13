using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Admin;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Common;

namespace Zuppeto.Api.Endpoints;

internal static class PetGovernanceEndpoints
{
    public static IEndpointRouteBuilder MapPetGovernanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/pet-governance").RequireAuthorization().WithTags("Pet-friendly governance");
        group.MapGet("/history", ListHistory);
        group.MapGet("/places/{placeId:guid}", GetOverview);
        group.MapPost("/places/{placeId:guid}/assertions", AddAssertion);
        group.MapPost("/places/{placeId:guid}/assertions/{assertionId:guid}/verify", VerifyAssertion);
        group.MapPost("/places/{placeId:guid}/assertions/{assertionId:guid}/decision", DecideAssertion);
        group.MapPost("/places/{placeId:guid}/conflicts/resolve", ResolveConflict);
        group.MapGet("/chains", ListChains);
        group.MapPost("/chains", CreateChain);
        group.MapPost("/chains/{chainId:guid}/policies", AddChainPolicy);
        group.MapPut("/places/{placeId:guid}/chain", LinkPlace);
        group.MapPost("/places/{placeId:guid}/chain-exceptions", AddLocalException);
        return app;
    }

    private static Task<IResult> GetOverview(Guid placeId, ClaimsPrincipal principal, IAdminApplicationService admin,
        IPetGovernanceApplicationService service, CancellationToken ct) =>
        Execute(principal, admin, actor => service.GetOverviewAsync(placeId, ct), ct);

    private static Task<IResult> ListHistory(Guid? placeId, ClaimsPrincipal principal, IAdminApplicationService admin,
        IPetGovernanceApplicationService service, CancellationToken ct) =>
        Execute(principal, admin, _ => service.ListHistoryAsync(placeId, ct), ct);

    private static Task<IResult> AddAssertion(Guid placeId, CreatePetAssertionRequest request, ClaimsPrincipal principal,
        IAdminApplicationService admin, IPetGovernanceApplicationService service, CancellationToken ct) =>
        Execute(principal, admin, actor => service.AddAssertionAsync(placeId, request, actor, ct), ct, created: true);

    private static Task<IResult> VerifyAssertion(Guid placeId, Guid assertionId, VerifyPetAssertionRequest request,
        ClaimsPrincipal principal, IAdminApplicationService admin, IPetGovernanceApplicationService service, CancellationToken ct) =>
        Execute(principal, admin, async actor => { await service.VerifyAssertionAsync(placeId, assertionId, request, actor, ct); return true; }, ct);

    private static Task<IResult> DecideAssertion(Guid placeId, Guid assertionId, DecidePetAssertionRequest request,
        ClaimsPrincipal principal, IAdminApplicationService admin, IPetGovernanceApplicationService service, CancellationToken ct) =>
        Execute(principal, admin, async actor => { await service.DecideAssertionAsync(placeId, assertionId, request, actor, ct); return true; }, ct);

    private static Task<IResult> ResolveConflict(Guid placeId, ResolvePetConflictRequest request,
        ClaimsPrincipal principal, IAdminApplicationService admin, IPetGovernanceApplicationService service, CancellationToken ct) =>
        Execute(principal, admin, async actor => { await service.ResolveConflictAsync(placeId, request, actor, ct); return true; }, ct);

    private static Task<IResult> ListChains(ClaimsPrincipal principal, IAdminApplicationService admin,
        IPetGovernanceApplicationService service, CancellationToken ct) =>
        Execute(principal, admin, _ => service.ListChainsAsync(ct), ct);

    private static Task<IResult> CreateChain(CreateCommercialChainRequest request, ClaimsPrincipal principal,
        IAdminApplicationService admin, IPetGovernanceApplicationService service, CancellationToken ct) =>
        Execute(principal, admin, actor => service.CreateChainAsync(request, actor, ct), ct, created: true);

    private static Task<IResult> AddChainPolicy(Guid chainId, CreateChainPetPolicyRequest request, ClaimsPrincipal principal,
        IAdminApplicationService admin, IPetGovernanceApplicationService service, CancellationToken ct) =>
        Execute(principal, admin, actor => service.AddChainPolicyAsync(chainId, request, actor, ct), ct, created: true);

    private static Task<IResult> LinkPlace(Guid placeId, LinkPlaceToChainRequest request, ClaimsPrincipal principal,
        IAdminApplicationService admin, IPetGovernanceApplicationService service, CancellationToken ct) =>
        Execute(principal, admin, async actor => { await service.LinkPlaceAsync(placeId, request, actor, ct); return true; }, ct);

    private static Task<IResult> AddLocalException(Guid placeId, CreateLocalChainExceptionRequest request,
        ClaimsPrincipal principal, IAdminApplicationService admin, IPetGovernanceApplicationService service, CancellationToken ct) =>
        Execute(principal, admin, actor => service.AddLocalExceptionAsync(placeId, request, actor, ct), ct, created: true);

    private static async Task<IResult> Execute<T>(ClaimsPrincipal principal, IAdminApplicationService admin,
        Func<Guid, Task<T>> action, CancellationToken ct, bool created = false)
    {
        var actor = principal.GetCurrentUserId();
        var hasPermission = actor.HasValue
            && await principal.HasPermissionAsync(admin, "action.places.manage", ct);
        if (!PetGovernanceAuthorizationPolicy.Allows(actor, principal.GetCurrentRoleKey(), hasPermission))
            return TypedResults.Forbid();
        var actorId = actor.GetValueOrDefault();
        try
        {
            var result = await action(actorId);
            return created ? TypedResults.Created(string.Empty, result) : TypedResults.Ok(result);
        }
        catch (KeyNotFoundException) { return TypedResults.NotFound(); }
        catch (DomainRuleException ex) { return TypedResults.BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return TypedResults.Conflict(new { message = ex.Message }); }
        catch (DbUpdateException) { return TypedResults.Conflict(new { message = "La decisió entra en conflicte amb l'estat actual." }); }
    }
}
