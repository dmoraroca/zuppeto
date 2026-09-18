using Zuppeto.Application.Users;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Users;

namespace Zuppeto.Application.Auth;

internal sealed class AuthApplicationService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITotpService totpService,
    ITotpRecoveryCodeRepository recoveryCodes,
    ITwoFactorChallengeStore challenges,
    IAccessTokenRevocationStore accessTokenRevocations,
    IAuthSessionFactory sessionFactory,
    IFederatedAuthenticationService federatedAuthentication,
    IExternalIdentityLinkingService externalIdentityLinkingService,
    IGoogleIdTokenVerifier googleIdTokenVerifier,
    IFacebookOAuthClient facebookOAuthClient) : IAuthApplicationService
{
    public async Task EndSessionAsync(
        Guid userId,
        string? tokenId,
        DateTimeOffset? tokenExpiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        challenges.RevokeForUser(userId);

        if (!string.IsNullOrWhiteSpace(tokenId) && tokenExpiresAtUtc is not null)
        {
            await accessTokenRevocations.RevokeAsync(
                userId,
                tokenId,
                tokenExpiresAtUtc.Value,
                cancellationToken);
            return;
        }

        // Transitional fallback for JWTs issued before per-session jti support.
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) return;
        user.RevokeSessions();
        await userRepository.UpdateAsync(user, cancellationToken);
    }

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
        return LoginResult.Success(await sessionFactory.CreateAsync(user, cancellationToken: cancellationToken));
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
        return await sessionFactory.CreateAsync(user, challenge.Provider, challenge.RequiresProfileCompletion, cancellationToken);
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

        return await federatedAuthentication.SignInAsync(
            identity,
            new("google", googleIdTokenVerifier.AdminEmails, ImportProviderAvatar: false),
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

        var login = await federatedAuthentication.SignInAsync(
            exchange.Value.Identity,
            new("facebook", facebookOAuthClient.AdminEmails, LinkExistingUserByVerifiedEmail: false),
            cancellationToken);

        return new AuthCallbackResult(login, exchange.Value.RedirectTo);
    }

    public async Task<AuthSessionDto?> GetSessionByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : await sessionFactory.CreateAsync(user, cancellationToken: cancellationToken);
    }

    public IReadOnlyCollection<AuthProviderDto> GetProviders()
    {
        return
        [
            new("password", "Credencials pròpies", "password", true),
            new("google", "Google", "oidc", googleIdTokenVerifier.IsConfigured, googleIdTokenVerifier.ClientId),
            new("facebook", "Facebook", "oauth2", facebookOAuthClient.IsConfigured, facebookOAuthClient.AppId)
        ];
    }

}
