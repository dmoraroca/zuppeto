using YepPet.Application.Users;
using YepPet.Domain.Abstractions;
using YepPet.Domain.Users;
using YepPet.Domain.Users.ValueObjects;

namespace YepPet.Application.Auth;

internal sealed class AuthApplicationService(
    IUserRepository userRepository,
    IRolePermissionRepository rolePermissionRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer accessTokenIssuer,
    IGoogleIdTokenVerifier googleIdTokenVerifier,
    ILinkedInOAuthClient linkedInOAuthClient,
    IFacebookOAuthClient facebookOAuthClient) : IAuthApplicationService
{
    public async Task<AuthSessionDto?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return null;
        }

        user.RecordAccess(DateTimeOffset.UtcNow);
        await userRepository.UpdateAsync(user, cancellationToken);
        return await CreateSessionAsync(user, cancellationToken: cancellationToken);
    }

    public async Task<AuthSessionDto?> LoginWithGoogleAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var identity = await googleIdTokenVerifier.VerifyAsync(request.IdToken, cancellationToken);

        if (identity is null || !identity.EmailVerified)
        {
            return null;
        }

        return await LoginWithFederatedIdentityAsync(
            identity,
            "google",
            "Google",
            googleIdTokenVerifier.AdminEmails,
            cancellationToken);
    }

    public string? GetLinkedInAuthorizationUrl(string? redirectTo = null)
    {
        return linkedInOAuthClient.BuildAuthorizationUrl(redirectTo);
    }

    public async Task<AuthCallbackResult?> LoginWithLinkedInAsync(
        LinkedInOAuthCallbackRequest request,
        CancellationToken cancellationToken = default)
    {
        var exchange = await linkedInOAuthClient.ExchangeCodeAsync(request.Code, request.State, cancellationToken);

        if (exchange is null || !exchange.Value.Identity.EmailVerified)
        {
            return null;
        }

        var session = await LoginWithFederatedIdentityAsync(
            exchange.Value.Identity,
            "linkedin",
            "LinkedIn",
            linkedInOAuthClient.AdminEmails,
            cancellationToken);

        return session is null ? null : new AuthCallbackResult(session, exchange.Value.RedirectTo);
    }

    public string? GetFacebookAuthorizationUrl(string? redirectTo = null)
    {
        return facebookOAuthClient.BuildAuthorizationUrl(redirectTo);
    }

    public async Task<AuthCallbackResult?> LoginWithFacebookAsync(
        FacebookOAuthCallbackRequest request,
        CancellationToken cancellationToken = default)
    {
        var exchange = await facebookOAuthClient.ExchangeCodeAsync(request.Code, request.State, cancellationToken);

        if (exchange is null || !exchange.Value.Identity.EmailVerified)
        {
            return null;
        }

        var session = await LoginWithFederatedIdentityAsync(
            exchange.Value.Identity,
            "facebook",
            "Facebook",
            facebookOAuthClient.AdminEmails,
            cancellationToken);

        return session is null ? null : new AuthCallbackResult(session, exchange.Value.RedirectTo);
    }

    public async Task<AuthSessionDto?> GetSessionByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : await CreateSessionAsync(user, cancellationToken: cancellationToken);
    }

    public IReadOnlyCollection<AuthProviderDto> GetProviders()
    {
        return
        [
            new("password", "Credencials pròpies", "password", true),
            new("google", "Google", "oidc", googleIdTokenVerifier.IsConfigured, googleIdTokenVerifier.ClientId),
            new("linkedin", "LinkedIn", "oidc", linkedInOAuthClient.IsConfigured, linkedInOAuthClient.ClientId),
            new("facebook", "Facebook", "oauth2", facebookOAuthClient.IsConfigured, facebookOAuthClient.AppId)
        ];
    }

    private async Task<AuthSessionDto> CreateSessionAsync(
        User user,
        string provider = "password",
        CancellationToken cancellationToken = default)
    {
        var token = accessTokenIssuer.Issue(user);
        var permissionKeys = await rolePermissionRepository.GetPermissionKeysByRoleAsync(user.Role, cancellationToken);

        return new AuthSessionDto(
            token.Token,
            token.ExpiresAtUtc,
            provider,
            new UserDto(
                user.Id,
                user.Email,
                user.Role,
                user.Profile.DisplayName,
                user.Profile.City,
                user.Profile.Country,
                user.Profile.Bio,
                user.Profile.AvatarUrl,
                user.PrivacyConsent.Accepted,
                user.PrivacyConsent.AcceptedAtUtc),
            permissionKeys);
    }

    private async Task<AuthSessionDto?> LoginWithFederatedIdentityAsync(
        FederatedIdentityPayload identity,
        string providerKey,
        string providerDisplayName,
        IReadOnlyCollection<string> adminEmails,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(identity.Email, cancellationToken);
        var shouldBeAdmin = IsAdminEmail(identity.Email, adminEmails);

        if (user is null)
        {
            user = new User(
                Guid.NewGuid(),
                identity.Email,
                passwordHasher.Hash(Guid.NewGuid().ToString("N")),
                shouldBeAdmin ? "Admin" : "Viewer",
                new UserProfile(
                    ResolveDisplayName(identity),
                    string.Empty,
                    string.Empty,
                    $"Perfil creat a través de {providerDisplayName}.",
                    identity.AvatarUrl),
                shouldBeAdmin
                    ? new PrivacyConsent(true, DateTimeOffset.UtcNow)
                    : new PrivacyConsent(false, null),
                null,
                DateTimeOffset.UtcNow);

            await userRepository.AddAsync(user, cancellationToken);
        }
        else
        {
            var shouldPersist = false;

            if (shouldBeAdmin && !string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                user.ChangeRole("Admin");

                if (!user.PrivacyConsent.Accepted)
                {
                    user.AcceptPrivacy(DateTimeOffset.UtcNow);
                }

                shouldPersist = true;
            }

            if (CanSynchronizeProfile(user) && ShouldSynchronizeProfile(user, identity))
            {
                user.UpdateProfile(
                    new UserProfile(
                        ResolveDisplayName(identity),
                        user.Profile.City,
                        user.Profile.Country,
                        string.IsNullOrWhiteSpace(user.Profile.Bio)
                            ? $"Perfil sincronitzat a través de {providerDisplayName}."
                            : user.Profile.Bio,
                        string.IsNullOrWhiteSpace(identity.AvatarUrl) ? user.Profile.AvatarUrl : identity.AvatarUrl));
                shouldPersist = true;
            }

            if (shouldPersist)
            {
                await userRepository.UpdateAsync(user, cancellationToken);
            }

            user.RecordAccess(DateTimeOffset.UtcNow);
            await userRepository.UpdateAsync(user, cancellationToken);
        }

        return await CreateSessionAsync(user, providerKey, cancellationToken);
    }

    private static bool IsAdminEmail(string email, IReadOnlyCollection<string> adminEmails)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return adminEmails.Any(candidate => candidate == normalizedEmail);
    }

    private static string ResolveDisplayName(FederatedIdentityPayload identity)
    {
        return string.IsNullOrWhiteSpace(identity.DisplayName) ? identity.Email : identity.DisplayName;
    }

    private static bool CanSynchronizeProfile(User user)
    {
        return string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(user.Role, "Developer", StringComparison.OrdinalIgnoreCase)
            || user.PrivacyConsent.Accepted;
    }

    private static bool ShouldSynchronizeProfile(User user, FederatedIdentityPayload identity)
    {
        var nextDisplayName = ResolveDisplayName(identity);
        var nextAvatarUrl = string.IsNullOrWhiteSpace(identity.AvatarUrl) ? user.Profile.AvatarUrl : identity.AvatarUrl;

        return !string.Equals(user.Profile.DisplayName, nextDisplayName, StringComparison.Ordinal) ||
               !string.Equals(user.Profile.AvatarUrl, nextAvatarUrl, StringComparison.Ordinal);
    }
}
