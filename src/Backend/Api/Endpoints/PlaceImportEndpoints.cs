using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Admin;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Common;

namespace Zuppeto.Api.Endpoints;

internal static class PlaceImportEndpoints
{
    public static IEndpointRouteBuilder MapPlaceImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group=app.MapGroup("/api/admin/place-imports").RequireAuthorization().WithTags("Place imports");
        group.MapGet("/context",Context);
        group.MapGet("/",List);
        group.MapGet("/{id:guid}",Get);
        group.MapPost("/",Prepare);
        group.MapPost("/{id:guid}/approve",Approve);
        group.MapPost("/{id:guid}/simulate-publication",SimulatePublication);
        group.MapPost("/{id:guid}/publish",Publish);
        group.MapPost("/{id:guid}/cancel",Cancel);
        return app;
    }

    private static Task<IResult> Context(ClaimsPrincipal principal,IAdminApplicationService admin,IEnumerable<IPlaceDiscoverySource> sources,CancellationToken ct)=>
        Execute(principal,admin,_=>Task.FromResult<object>(new
        {
            sources=sources.Select(source=>new{key=source.Descriptor.AccessMode,label=source.Descriptor.AccessMode switch
            {
                OpenStreetMapSource.GeofabrikExtractAccessMode => "OpenStreetMap / extracte Geofabrik",
                OpenStreetMapSource.DirectMapAccessMode => "OpenStreetMap / API directa (auditoria)",
                _ => "OpenStreetMap / Overpass"
            },license=source.Descriptor.LicenseIdentifier,attribution=source.Descriptor.Attribution}),
            countries=new[]{"ES"},categories=new[]{
                new{key="restaurant",label="Restaurants"},new{key="cafe",label="Cafeteries"},new{key="bar",label="Bars"},new{key="pub",label="Pubs"},
                new{key="fast_food",label="Menjar ràpid"},new{key="biergarten",label="Cerveseries a l’aire lliure"},
                new{key="food_court",label="Zones de restauració"},new{key="nightclub",label="Locals nocturns"},
                new{key="ice_cream",label="Gelateries"},new{key="bakery",label="Fleques"},new{key="pharmacy",label="Farmàcies"},
                new{key="fuel",label="Benzineres"},new{key="hotel",label="Hotels"},new{key="hostel",label="Hostals i pensions"},
                new{key="motel",label="Motels"},new{key="apartment",label="Apartaments turístics"},new{key="camping",label="Càmpings"},
                new{key="park",label="Parcs"},new{key="dog_park",label="Parcs canins"},new{key="shop",label="Comerços"},
                new{key="pet_shop",label="Botigues per mascotes"},new{key="veterinary",label="Veterinaris"},
                new{key="pet_service",label="Serveis per mascotes"},new{key="pet_grooming",label="Perruqueries canines"}},simulationOnly=true
        }),ct);
    private static Task<IResult> List(ClaimsPrincipal p,IAdminApplicationService a,IPlaceImportApplicationService s,CancellationToken ct)=>Execute(p,a,_=>s.ListAsync(ct),ct);
    private static async Task<IResult> Get(Guid id,ClaimsPrincipal p,IAdminApplicationService a,IPlaceImportApplicationService s,CancellationToken ct)
    {
        var result=await ExecuteValue(p,a,_=>s.GetAsync(id,ct),ct);return result;
    }
    private static Task<IResult> Prepare(PreparePlaceImportRequest request,ClaimsPrincipal p,IAdminApplicationService a,IPlaceImportApplicationService s,CancellationToken ct)=>Execute(p,a,actor=>s.PrepareAsync(request,actor,ct),ct,true);
    private static Task<IResult> Approve(Guid id,PlaceImportDecisionRequest request,ClaimsPrincipal p,IAdminApplicationService a,IPlaceImportApplicationService s,CancellationToken ct)=>Execute(p,a,async actor=>{await s.ApproveAsync(id,request.Reason,actor,ct);return true;},ct);
    private static Task<IResult> SimulatePublication(Guid id,PlaceImportDecisionRequest request,ClaimsPrincipal p,IAdminApplicationService a,IPlaceImportApplicationService s,CancellationToken ct)=>Execute(p,a,async actor=>{await s.SimulatePublicationAsync(id,request.Reason,actor,ct);return true;},ct);
    private static Task<IResult> Publish(Guid id,PlaceImportDecisionRequest request,ClaimsPrincipal p,IAdminApplicationService a,IPlaceImportApplicationService s,CancellationToken ct)=>Execute(p,a,async actor=>{await s.PublishAsync(id,request.Reason,actor,ct);return true;},ct);
    private static Task<IResult> Cancel(Guid id,ClaimsPrincipal p,IAdminApplicationService a,IPlaceImportApplicationService s,CancellationToken ct)=>Execute(p,a,async actor=>{await s.CancelAsync(id,actor,ct);return true;},ct);

    private static async Task<IResult> Execute<T>(ClaimsPrincipal principal,IAdminApplicationService admin,Func<Guid,Task<T>> action,CancellationToken ct,bool created=false)
    {
        var actor=principal.GetCurrentUserId();var allowed=actor.HasValue&&string.Equals(principal.GetCurrentRoleKey(),"Admin",StringComparison.OrdinalIgnoreCase)&&await principal.HasPermissionAsync(admin,"action.places.manage",ct);
        if(!allowed)return TypedResults.Forbid();
        try{var value=await action(actor!.Value);return created?TypedResults.Accepted(string.Empty,value):TypedResults.Ok(value);}
        catch(KeyNotFoundException){return TypedResults.NotFound();}catch(DomainRuleException ex){return TypedResults.BadRequest(new{message=ex.Message});}catch(InvalidOperationException ex){return TypedResults.Conflict(new{message=ex.Message});}catch(DbUpdateException){return TypedResults.Conflict(new{message="La importació entra en conflicte amb l’estat actual."});}
    }
    private static async Task<IResult> ExecuteValue<T>(ClaimsPrincipal p,IAdminApplicationService a,Func<Guid,Task<T?>> action,CancellationToken ct) where T:class
    {
        var actor=p.GetCurrentUserId();var allowed=actor.HasValue&&string.Equals(p.GetCurrentRoleKey(),"Admin",StringComparison.OrdinalIgnoreCase)&&await p.HasPermissionAsync(a,"action.places.manage",ct);if(!allowed)return TypedResults.Forbid();
        var value=await action(actor!.Value);return value is null?TypedResults.NotFound():TypedResults.Ok(value);
    }
}

internal sealed record PlaceImportDecisionRequest(string Reason);
