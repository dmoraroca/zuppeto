using Zuppeto.Application.Users;

namespace Zuppeto.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public enum LoginFailureReason
{
    InvalidCredentials,
    EmailActivationRequired,
    TwoFactorRequired,
    FederatedProviderUnavailable,
    FederatedIdentityRejected,
    ExternalIdentityLinkRequired
}

public sealed record LoginResult(AuthSessionDto? Session, LoginFailureReason? FailureReason, string? ChallengeId = null)
{
    public static LoginResult InvalidCredentials() => new(null, LoginFailureReason.InvalidCredentials);
    public static LoginResult ActivationRequired() => new(null, LoginFailureReason.EmailActivationRequired);
    public static LoginResult TwoFactorRequired(string challengeId) => new(null, LoginFailureReason.TwoFactorRequired, challengeId);
    public static LoginResult FederatedProviderUnavailable() => new(null, LoginFailureReason.FederatedProviderUnavailable);
    public static LoginResult FederatedIdentityRejected() => new(null, LoginFailureReason.FederatedIdentityRejected);
    public static LoginResult ExternalIdentityLinkRequired() => new(null, LoginFailureReason.ExternalIdentityLinkRequired);
    public static LoginResult Success(AuthSessionDto session) => new(session, null);
}
public sealed record TwoFactorLoginRequest(string ChallengeId, string Code);

public sealed record GoogleLoginRequest(string IdToken);
public sealed record FacebookOAuthCallbackRequest(string Code, string State);

public sealed record AuthSessionDto(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string Provider,
    UserDto User,
    IReadOnlyCollection<string> PermissionKeys,
    bool RequiresProfileCompletion = false);

public sealed record AuthCallbackResult(
    LoginResult Login,
    string? RedirectTo);

public sealed record AuthProviderDto(
    string Key,
    string DisplayName,
    string Protocol,
    bool Configured,
    string? ClientId = null);

public sealed record AccessTokenResult(
    string Token,
    DateTimeOffset ExpiresAtUtc);

public sealed record FederatedIdentityPayload(
    string Provider,
    string Subject,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    bool EmailVerified);

public sealed record AccessMethodDto(
    string Provider,
    string DisplayName,
    bool Linked,
    bool Available,
    string Status);

public sealed record AccessMethodsDto(IReadOnlyCollection<AccessMethodDto> Methods);

public enum ExternalIdentityLinkFailureReason
{
    UserNotFound,
    ProviderUnavailable,
    IdentityRejected,
    EmailMismatch,
    IdentityLinkedToAnotherUser,
    ProviderAlreadyLinkedToDifferentIdentity,
    Conflict
}

public sealed record ExternalIdentityLinkResult(
    bool Linked,
    bool AlreadyLinked,
    ExternalIdentityLinkFailureReason? FailureReason)
{
    public static ExternalIdentityLinkResult Success(bool alreadyLinked = false) => new(true, alreadyLinked, null);
    public static ExternalIdentityLinkResult Failure(ExternalIdentityLinkFailureReason reason) => new(false, false, reason);
}
