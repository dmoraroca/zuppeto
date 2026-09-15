namespace Zuppeto.Application.Auth;

public interface IAuthApplicationService
{
    Task<AuthSessionDto?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<LoginResult> LoginWithResultAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthSessionDto?> CompleteTwoFactorLoginAsync(TwoFactorLoginRequest request, CancellationToken cancellationToken = default);

    Task<LoginResult?> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default);

    string? GetLinkedInAuthorizationUrl(string? redirectTo = null);

    Task<AuthCallbackResult?> LoginWithLinkedInAsync(
        LinkedInOAuthCallbackRequest request,
        CancellationToken cancellationToken = default);

    string? GetFacebookAuthorizationUrl(string? redirectTo = null);

    Task<AuthCallbackResult?> LoginWithFacebookAsync(
        FacebookOAuthCallbackRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthSessionDto?> GetSessionByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    IReadOnlyCollection<AuthProviderDto> GetProviders();
}
