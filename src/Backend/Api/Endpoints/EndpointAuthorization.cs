using System.Security.Claims;
using YepPet.Application.Admin;

namespace YepPet.Api.Endpoints;

internal static class EndpointAuthorization
{
    public static Guid? GetCurrentUserId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        return Guid.TryParse(subject, out var userId) ? userId : null;
    }

    /// <summary>Role key as stored in <c>roles.key</c> (JWT claim).</summary>
    public static string? GetCurrentRoleKey(this ClaimsPrincipal principal)
    {
        var roleValue = principal.FindFirstValue(ClaimTypes.Role)
            ?? principal.FindFirstValue("role");

        return string.IsNullOrWhiteSpace(roleValue) ? null : roleValue.Trim();
    }

    public static async Task<bool> HasPermissionAsync(
        this ClaimsPrincipal principal,
        IAdminApplicationService adminService,
        string permissionKey,
        CancellationToken cancellationToken = default)
    {
        var roleKey = principal.GetCurrentRoleKey();

        if (roleKey is null)
        {
            return false;
        }

        var permissionKeys = await adminService.GetPermissionKeysByRoleAsync(roleKey, cancellationToken);
        return permissionKeys.Contains(permissionKey, StringComparer.Ordinal);
    }
}
