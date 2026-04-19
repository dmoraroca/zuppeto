using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using YepPet.Application.Navigation;

namespace YepPet.Api.Endpoints;

internal static class NavigationEndpoints
{
    public static IEndpointRouteBuilder MapNavigationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/navigation").RequireAuthorization();
        group.MapGet("/menu", GetMenuAsync);
        return app;
    }

    [Authorize]
    private static async Task<Results<Ok<IReadOnlyCollection<NavigationMenuItemDto>>, UnauthorizedHttpResult>> GetMenuAsync(
        ClaimsPrincipal principal,
        INavigationApplicationService service,
        CancellationToken cancellationToken)
    {
        var roleKey = principal.FindFirstValue(ClaimTypes.Role) ?? principal.FindFirstValue("role");

        if (string.IsNullOrWhiteSpace(roleKey))
        {
            return TypedResults.Unauthorized();
        }

        var menu = await service.GetMenuForRoleAsync(roleKey.Trim(), cancellationToken);
        return TypedResults.Ok(menu);
    }
}
