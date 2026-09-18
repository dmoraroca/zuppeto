namespace Zuppeto.Application.Auth;

public interface IAuthApplicationService
{
    Task<AuthSessionDto?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<LoginResult> LoginWithResultAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthSessionDto?> CompleteTwoFactorLoginAsync(TwoFactorLoginRequest request, CancellationToken cancellationToken = default);

    Task<LoginResult> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default);

    Task<AccessMethodsDto?> GetAccessMethodsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ExternalIdentityLinkResult> LinkGoogleAsync(
        Guid userId,
        GoogleLoginRequest request,
        CancellationToken cancellationToken = default);

    string? GetFacebookAuthorizationUrl(string? redirectTo = null);

    Task<AuthCallbackResult?> LoginWithFacebookAsync(
        FacebookOAuthCallbackRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthSessionDto?> GetSessionByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task EndSessionAsync(
        Guid userId,
        string? tokenId,
        DateTimeOffset? tokenExpiresAtUtc,
        CancellationToken cancellationToken = default);

    IReadOnlyCollection<AuthProviderDto> GetProviders();
}
