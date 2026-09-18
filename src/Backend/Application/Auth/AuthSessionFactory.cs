using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Users;

namespace Zuppeto.Application.Auth;

internal sealed class AuthSessionFactory(
    IRolePermissionRepository rolePermissionRepository,
    IAccessTokenIssuer accessTokenIssuer) : IAuthSessionFactory
{
    public async Task<AuthSessionDto> CreateAsync(
        User user,
        string provider = "password",
        bool requiresProfileCompletion = false,
        CancellationToken cancellationToken = default)
    {
        var token = accessTokenIssuer.Issue(user);
        var permissionKeys = await rolePermissionRepository.GetPermissionKeysByRoleAsync(user.Role, cancellationToken);

        return new AuthSessionDto(
            token.Token,
            token.ExpiresAtUtc,
            provider,
            new(
                user.Id,
                user.Email,
                user.Role,
                user.Profile.DisplayName,
                user.Profile.City,
                user.Profile.Country,
                user.Profile.Comments,
                user.Profile.AvatarUrl,
                user.PrivacyConsent.Accepted,
                user.PrivacyConsent.AcceptedAtUtc,
                user.HasLocalCredential,
                user.IsTotpEnabled),
            permissionKeys,
            requiresProfileCompletion);
    }
}
