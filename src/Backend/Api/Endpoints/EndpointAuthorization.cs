using System.Security.Claims;
using YepPet.Application.Admin;
using YepPet.Domain.Users;

namespace YepPet.Api.Endpoints;

internal static class EndpointAuthorization
{
    public static Guid? GetCurrentUserId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        return Guid.TryParse(subject, out var userId) ? userId : null;
    }

    public static UserRole? GetCurrentRole(this ClaimsPrincipal principal)
    {
        var roleValue = principal.FindFirstValue(ClaimTypes.Role)
            ?? principal.FindFirstValue("role");

        return Enum.TryParse<UserRole>(roleValue, ignoreCase: true, out var role)
            ? role
            : null;
    }

    public static async Task<bool> HasPermissionAsync(
        this ClaimsPrincipal principal,
        IAdminApplicationService adminService,
        string permissionKey,
        CancellationToken cancellationToken = default)
    {
        var role = principal.GetCurrentRole();

        if (role is null)
        {
            return false;
        }

        var permissionKeys = await adminService.GetPermissionKeysByRoleAsync(role.Value.ToString(), cancellationToken);
        return permissionKeys.Contains(permissionKey, StringComparer.Ordinal);
    }
}
