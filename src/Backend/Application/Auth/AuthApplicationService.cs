using Zuppeto.Application.Users;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Users;
using Zuppeto.Domain.Users.ValueObjects;

namespace Zuppeto.Application.Auth;

internal sealed class AuthApplicationService(
    IUserRepository userRepository,
    IExternalIdentityRepository externalIdentityRepository,
    IRolePermissionRepository rolePermissionRepository,
    IPasswordHasher passwordHasher,
    ITotpService totpService,
    ITotpRecoveryCodeRepository recoveryCodes,
    ITwoFactorChallengeStore challenges,
    IAccessTokenIssuer accessTokenIssuer,
    IExternalIdentityLinkingService externalIdentityLinkingService,
    IGoogleIdTokenVerifier googleIdTokenVerifier,
    ILinkedInOAuthClient linkedInOAuthClient,
    IFacebookOAuthClient facebookOAuthClient) : IAuthApplicationService
{
    public async Task<AuthSessionDto?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        return (await LoginWithResultAsync(request, cancellationToken)).Session;
    }

    public async Task<LoginResult> LoginWithResultAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !user.HasLocalCredential || !passwordHasher.Verify(user.PasswordHash!, request.Password))
        {
            return LoginResult.InvalidCredentials();
        }

        if (!user.IsEmailActivated)
        {
            return LoginResult.ActivationRequired();
        }
        if (user.IsTotpEnabled) return LoginResult.TwoFactorRequired(challenges.Create(user.Id, "password"));

        user.RecordAccess(DateTimeOffset.UtcNow);
        await userRepository.UpdateAsync(user, cancellationToken);
        return LoginResult.Success(await CreateSessionAsync(user, cancellationToken: cancellationToken));
    }

    public async Task<AuthSessionDto?> CompleteTwoFactorLoginAsync(TwoFactorLoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ChallengeId) || !challenges.TryConsume(request.ChallengeId, out var challenge)) return null;
        var user = await userRepository.GetByIdAsync(challenge.UserId, cancellationToken);
        if (user is null || !user.IsTotpEnabled || user.TotpSecretProtected is null) return null;
        var verification = totpService.Verify(user.TotpSecretProtected, request.Code, DateTimeOffset.UtcNow);
        var accepted = verification.IsValid
            ? user.TryUseTotpTimeStep(verification.TimeStepMatched) && challenges.TryUseTotpTimeStep(user.Id, verification.TimeStepMatched)
            : await recoveryCodes.ConsumeAsync(user.Id, totpService.HashRecoveryCode(request.Code), cancellationToken);
        if (!accepted) return null;
        user.RecordAccess(DateTimeOffset.UtcNow); await userRepository.UpdateAsync(user, cancellationToken);
        return await CreateSessionAsync(user, challenge.Provider, challenge.RequiresProfileCompletion, cancellationToken);
    }

    public async Task<LoginResult> LoginWithGoogleAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!googleIdTokenVerifier.IsConfigured)
        {
            return LoginResult.FederatedProviderUnavailable();
        }

        var identity = await googleIdTokenVerifier.VerifyAsync(request.IdToken, cancellationToken);

        if (identity is null || !identity.EmailVerified)
        {
            return LoginResult.FederatedIdentityRejected();
        }

        return await LoginWithFederatedIdentityAsync(
            identity,
            "google",
            "Google",
            googleIdTokenVerifier.AdminEmails,
            linkExistingUserByVerifiedEmail: true,
            cancellationToken);
    }

    public Task<AccessMethodsDto?> GetAccessMethodsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return externalIdentityLinkingService.GetAccessMethodsAsync(userId, cancellationToken);
    }

    public async Task<ExternalIdentityLinkResult> LinkGoogleAsync(
        Guid userId,
        GoogleLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!googleIdTokenVerifier.IsConfigured)
        {
            return ExternalIdentityLinkResult.Failure(ExternalIdentityLinkFailureReason.ProviderUnavailable);
        }

        var identity = await googleIdTokenVerifier.VerifyAsync(request.IdToken, cancellationToken);
        if (identity is null || !identity.EmailVerified || !string.Equals(identity.Provider, "google", StringComparison.OrdinalIgnoreCase))
        {
            return ExternalIdentityLinkResult.Failure(ExternalIdentityLinkFailureReason.IdentityRejected);
        }

        return await externalIdentityLinkingService.LinkAsync(userId, identity, cancellationToken);
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

        var login = await LoginWithFederatedIdentityAsync(
            exchange.Value.Identity,
            "linkedin",
            "LinkedIn",
            linkedInOAuthClient.AdminEmails,
            linkExistingUserByVerifiedEmail: false,
            cancellationToken);

        return login is null ? null : new AuthCallbackResult(login, exchange.Value.RedirectTo);
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

        var login = await LoginWithFederatedIdentityAsync(
            exchange.Value.Identity,
            "facebook",
            "Facebook",
            facebookOAuthClient.AdminEmails,
            linkExistingUserByVerifiedEmail: false,
            cancellationToken);

        return login is null ? null : new AuthCallbackResult(login, exchange.Value.RedirectTo);
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
        bool requiresProfileCompletion = false,
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
                user.Profile.Comments,
                user.Profile.AvatarUrl,
                user.PrivacyConsent.Accepted,
                user.PrivacyConsent.AcceptedAtUtc,
                user.HasLocalCredential,
                user.IsTotpEnabled),
            permissionKeys,
            requiresProfileCompletion);
    }

    private async Task<LoginResult> LoginWithFederatedIdentityAsync(
        FederatedIdentityPayload identity,
        string providerKey,
        string providerDisplayName,
        IReadOnlyCollection<string> adminEmails,
        bool linkExistingUserByVerifiedEmail,
        CancellationToken cancellationToken)
    {
        var externalIdentity = await externalIdentityRepository.GetByProviderAndSubjectAsync(identity.Provider, identity.Subject, cancellationToken);
        var user = externalIdentity is null
            ? await userRepository.GetByEmailAsync(identity.Email, cancellationToken)
            : await userRepository.GetByIdAsync(externalIdentity.UserId, cancellationToken);
        var shouldBeAdmin = IsAdminEmail(identity.Email, adminEmails);
        var firstFederatedLogin = false;

        if (user is null)
        {
            user = new User(
                Guid.NewGuid(),
                identity.Email,
                null,
                shouldBeAdmin ? "Admin" : "User",
                new UserProfile(
                    ResolveDisplayName(identity),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    ResolveFederatedAvatarUrl(providerKey, identity.AvatarUrl, null)),
                shouldBeAdmin
                    ? new PrivacyConsent(true, DateTimeOffset.UtcNow)
                    : new PrivacyConsent(false, null),
                null,
                DateTimeOffset.UtcNow);

            await userRepository.AddAsync(user, cancellationToken);
            await externalIdentityRepository.AddAsync(new ExternalIdentity(Guid.NewGuid(), user.Id, providerKey, identity.Subject, DateTimeOffset.UtcNow), cancellationToken);
            firstFederatedLogin = true;
        }
        else
        {
            if (externalIdentity is null)
            {
                if (!linkExistingUserByVerifiedEmail || !identity.EmailVerified || !user.IsEmailActivated)
                {
                    return LoginResult.ExternalIdentityLinkRequired();
                }

                var providerIdentity = await externalIdentityRepository.GetByUserAndProviderAsync(
                    user.Id, providerKey, cancellationToken);
                if (providerIdentity is not null)
                {
                    return LoginResult.ExternalIdentityLinkRequired();
                }

                var linked = await externalIdentityRepository.TryAddAsync(
                    new ExternalIdentity(Guid.NewGuid(), user.Id, providerKey, identity.Subject, DateTimeOffset.UtcNow),
                    cancellationToken);
                if (!linked)
                {
                    var racedIdentity = await externalIdentityRepository.GetByProviderAndSubjectAsync(
                        providerKey, identity.Subject, cancellationToken);
                    if (racedIdentity?.UserId != user.Id)
                    {
                        return LoginResult.ExternalIdentityLinkRequired();
                    }
                }
            }
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

            if (CanSynchronizeProfile(user) && ShouldSynchronizeProfile(user, identity, providerKey))
            {
                user.UpdateProfile(
                    new UserProfile(
                        ResolveDisplayName(identity),
                        user.Profile.City,
                        user.Profile.Country,
                        user.Profile.Comments,
                        ResolveFederatedAvatarUrl(providerKey, identity.AvatarUrl, user.Profile.AvatarUrl)));
                shouldPersist = true;
            }

            if (shouldPersist)
            {
                await userRepository.UpdateAsync(user, cancellationToken);
            }

            user.RecordAccess(DateTimeOffset.UtcNow);
            await userRepository.UpdateAsync(user, cancellationToken);
        }

        var requiresProfileCompletion = firstFederatedLogin || IsProfileIncomplete(user);
        return user.IsTotpEnabled
            ? LoginResult.TwoFactorRequired(challenges.Create(user.Id, providerKey, requiresProfileCompletion))
            : LoginResult.Success(await CreateSessionAsync(user, providerKey, requiresProfileCompletion, cancellationToken));
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

    private static bool ShouldSynchronizeProfile(User user, FederatedIdentityPayload identity, string providerKey)
    {
        var nextDisplayName = ResolveDisplayName(identity);
        var nextAvatarUrl = ResolveFederatedAvatarUrl(providerKey, identity.AvatarUrl, user.Profile.AvatarUrl);

        return !string.Equals(user.Profile.DisplayName, nextDisplayName, StringComparison.Ordinal) ||
               !string.Equals(user.Profile.AvatarUrl, nextAvatarUrl, StringComparison.Ordinal);
    }

    private static string? ResolveFederatedAvatarUrl(
        string providerKey,
        string? providerAvatarUrl,
        string? currentAvatarUrl)
    {
        // Google can expose its generated letter monogram through the same `picture`
        // claim as a real photo. Keep avatar ownership in Petiloc so that an absent
        // user-uploaded photo is represented consistently as unavailable.
        if (string.Equals(providerKey, "google", StringComparison.OrdinalIgnoreCase))
        {
            return currentAvatarUrl;
        }

        return string.IsNullOrWhiteSpace(providerAvatarUrl) ? currentAvatarUrl : providerAvatarUrl;
    }

    private static bool IsProfileIncomplete(User user) =>
        string.IsNullOrWhiteSpace(user.Profile.DisplayName) ||
        string.IsNullOrWhiteSpace(user.Profile.City) ||
        string.IsNullOrWhiteSpace(user.Profile.Country) ||
        (!string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase) && !user.PrivacyConsent.Accepted);
}
