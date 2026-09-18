using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.WebUtilities;
using Zuppeto.Application.Auth;
using Zuppeto.Application.Validation;
using Zuppeto.Api.Validation;
using Zuppeto.Application.Users;
using Zuppeto.Infrastructure.Email;

namespace Zuppeto.Api.Endpoints;

internal static class AuthEndpoints
{
    private static readonly JsonSerializerOptions CallbackJsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", LoginAsync);
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapPost("/login/totp", CompleteTwoFactorLoginAsync).RequireRateLimiting("totp");
        group.MapPost("/activation", ActivateEmailAsync);
        group.MapPost("/activation/resend", ResendActivationEmailAsync);
        group.MapPost("/password-recovery", RequestPasswordRecoveryAsync);
        group.MapPost("/password-reset", ResetPasswordAsync);
        group.MapPost("/totp/setup", StartTotpSetupAsync).RequireAuthorization();
        group.MapPost("/totp/setup/confirm", ConfirmTotpSetupAsync).RequireAuthorization().RequireRateLimiting("totp");
        group.MapPost("/totp/disable", DisableTotpAsync).RequireAuthorization().RequireRateLimiting("totp");
        group.MapPost("/totp/recovery-codes/regenerate", RegenerateTotpRecoveryCodesAsync).RequireAuthorization().RequireRateLimiting("totp");
        group.MapPost("/google", GoogleLoginAsync);
        group.MapGet("/access-methods", GetAccessMethodsAsync).RequireAuthorization();
        group.MapPost("/access-methods/google/link", LinkGoogleAsync).RequireAuthorization();
        group.MapGet("/facebook/start", FacebookStartAsync);
        group.MapGet("/facebook/callback", FacebookCallbackAsync);
        group.MapGet("/providers", GetProviders);
        group.MapGet("/me", GetCurrentSessionAsync).RequireAuthorization();

        return app;
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult>> LogoutAsync(
        ClaimsPrincipal principal,
        IAuthApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId)) return TypedResults.Unauthorized();
        var tokenId = principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti);
        var expirationClaim = principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp);
        DateTimeOffset? expiresAtUtc = long.TryParse(expirationClaim, out var expirationUnixSeconds)
            ? DateTimeOffset.FromUnixTimeSeconds(expirationUnixSeconds)
            : null;
        await service.EndSessionAsync(userId, tokenId, expiresAtUtc, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IValidator<LoginRequest> validator,
        IAuthApplicationService service,
        CancellationToken cancellationToken)
    {
        var validation = validator.Validate(request);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await service.LoginWithResultAsync(request, cancellationToken);
        if (result.Session is not null) return TypedResults.Ok(result.Session);
        if (result.FailureReason == LoginFailureReason.TwoFactorRequired) return TypedResults.Accepted("/api/auth/login/totp", new TwoFactorChallengeResponse(result.ChallengeId!));
        return result.FailureReason == LoginFailureReason.EmailActivationRequired
            ? TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Activació de compte necessària", detail: "Activa el compte des del correu abans d'iniciar sessió.")
            : TypedResults.Unauthorized();
    }

    private static async Task<Results<Ok<AuthSessionDto>, UnauthorizedHttpResult>> CompleteTwoFactorLoginAsync(TwoFactorLoginRequest request, IAuthApplicationService service, CancellationToken cancellationToken)
    {
        var session = await service.CompleteTwoFactorLoginAsync(request, cancellationToken);
        return session is null ? TypedResults.Unauthorized() : TypedResults.Ok(session);
    }
    private sealed record TwoFactorChallengeResponse(string ChallengeId);

    private static async Task<Ok<AccountActivationResult>> ActivateEmailAsync(
        AccountActivationRequest request,
        IUserApplicationService service,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await service.ActivateEmailAsync(request, cancellationToken));
    }

    private static async Task<Accepted> ResendActivationEmailAsync(
        ActivationEmailResendRequest request,
        IUserApplicationService service,
        CancellationToken cancellationToken)
    {
        await service.ResendActivationEmailAsync(request, cancellationToken);
        return TypedResults.Accepted("/api/auth/activation");
    }

    /// <summary>Local-only test adapter. No route is registered outside Development.</summary>
    public static IEndpointRouteBuilder MapDevelopmentActivationTestEndpoints(this IEndpointRouteBuilder app)
    {
        if (!app.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment()) return app;
        app.MapGet("/api/auth/activation/test-inbox/{email}", GetDevelopmentInboxToken);
        app.MapGet("/api/auth/password-recovery/test-inbox/{email}", GetDevelopmentPasswordRecoveryInboxToken);
        return app;
    }

    private static Results<Ok<DevelopmentActivationTokenDto>, NotFound> GetDevelopmentInboxToken(
        string email,
        DevelopmentActivationInbox inbox)
    {
        return inbox.TryTake(email, out var token)
            ? TypedResults.Ok(new DevelopmentActivationTokenDto(token))
            : TypedResults.NotFound();
    }

    private sealed record DevelopmentActivationTokenDto(string Token);

    private static Results<Ok<DevelopmentActivationTokenDto>, NotFound> GetDevelopmentPasswordRecoveryInboxToken(
        string email, DevelopmentPasswordRecoveryInbox inbox) => inbox.TryTake(email, out var token)
            ? TypedResults.Ok(new DevelopmentActivationTokenDto(token))
            : TypedResults.NotFound();

    private static async Task<Accepted> RequestPasswordRecoveryAsync(PasswordRecoveryRequest request, IUserApplicationService service, CancellationToken cancellationToken)
    {
        await service.RequestPasswordRecoveryAsync(request, cancellationToken);
        return TypedResults.Accepted("/api/auth/password-reset");
    }

    private static async Task<Ok<PasswordResetResult>> ResetPasswordAsync(PasswordResetRequest request, IUserApplicationService service, CancellationToken cancellationToken)
        => TypedResults.Ok(await service.ResetPasswordAsync(request, cancellationToken));

    private static async Task<Results<Ok<TotpSetupDto>, UnauthorizedHttpResult>> StartTotpSetupAsync(ClaimsPrincipal principal, IUserApplicationService service, CancellationToken cancellationToken)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(id, out var userId) ? TypedResults.Ok(await service.StartTotpSetupAsync(userId, cancellationToken)) : TypedResults.Unauthorized();
    }
    private static async Task<Results<Ok<TotpRecoveryCodesDto>, BadRequest>> ConfirmTotpSetupAsync(ClaimsPrincipal principal, TotpCodeRequest request, IUserApplicationService service, CancellationToken cancellationToken)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        var result = Guid.TryParse(id, out var userId) ? await service.ConfirmTotpSetupAsync(userId, request.Code, cancellationToken) : null;
        return result is null ? TypedResults.BadRequest() : TypedResults.Ok(result);
    }
    private static async Task<Results<NoContent, BadRequest>> DisableTotpAsync(ClaimsPrincipal principal, TotpCodeRequest request, IUserApplicationService service, CancellationToken cancellationToken)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(id, out var userId) && await service.DisableTotpAsync(userId, request.Code, cancellationToken) ? TypedResults.NoContent() : TypedResults.BadRequest();
    }
    private static async Task<Results<Ok<TotpRecoveryCodesDto>, BadRequest>> RegenerateTotpRecoveryCodesAsync(ClaimsPrincipal principal, TotpCodeRequest request, IUserApplicationService service, CancellationToken cancellationToken)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        var result = Guid.TryParse(id, out var userId) ? await service.RegenerateTotpRecoveryCodesAsync(userId, request.Code, cancellationToken) : null;
        return result is null ? TypedResults.BadRequest() : TypedResults.Ok(result);
    }
    private sealed record TotpCodeRequest(string Code);

    private static async Task<IResult> GoogleLoginAsync(
        GoogleLoginRequest request,
        IValidator<GoogleLoginRequest> validator,
        IAuthApplicationService service,
        CancellationToken cancellationToken)
    {
        var validation = validator.Validate(request);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await service.LoginWithGoogleAsync(request, cancellationToken);
        if (result.FailureReason == LoginFailureReason.FederatedProviderUnavailable)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Google no disponible",
                detail: "El proveïdor Google no està configurat.",
                extensions: new Dictionary<string, object?> { ["code"] = "federated_provider_unavailable" });
        }
        if (result.FailureReason == LoginFailureReason.ExternalIdentityLinkRequired)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Compte local existent",
                detail: "Ja existeix un compte local amb aquesta adreça, però no està vinculat a Google.",
                extensions: new Dictionary<string, object?> { ["code"] = "external_identity_link_required" });
        }
        if (result.FailureReason == LoginFailureReason.FederatedIdentityRejected)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Autenticació Google rebutjada",
                detail: "Google no ha pogut validar aquesta autenticació.",
                extensions: new Dictionary<string, object?> { ["code"] = "federated_identity_rejected" });
        }
        return result.Session is not null
            ? TypedResults.Ok(result.Session)
            : TypedResults.Accepted("/api/auth/login/totp", new TwoFactorChallengeResponse(result.ChallengeId!));
    }

    private static Ok<IReadOnlyCollection<AuthProviderDto>> GetProviders(IAuthApplicationService service)
    {
        return TypedResults.Ok(service.GetProviders());
    }

    private static async Task<IResult> GetAccessMethodsAsync(
        ClaimsPrincipal principal,
        IAuthApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId)) return TypedResults.Unauthorized();
        var methods = await service.GetAccessMethodsAsync(userId, cancellationToken);
        return methods is null ? TypedResults.NotFound() : TypedResults.Ok(methods);
    }

    private static async Task<IResult> LinkGoogleAsync(
        ClaimsPrincipal principal,
        GoogleLoginRequest request,
        IValidator<GoogleLoginRequest> validator,
        IAuthApplicationService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId)) return TypedResults.Unauthorized();
        var validation = validator.Validate(request);
        if (!validation.IsValid) return validation.ToValidationProblem();

        var result = await service.LinkGoogleAsync(userId, request, cancellationToken);
        if (result.Linked)
        {
            return TypedResults.Ok(new { provider = "google", linked = true, alreadyLinked = result.AlreadyLinked });
        }

        return result.FailureReason switch
        {
            ExternalIdentityLinkFailureReason.ProviderUnavailable => LinkProblem(
                StatusCodes.Status503ServiceUnavailable, "Google no disponible", "El proveïdor Google no està configurat.", "federated_provider_unavailable"),
            ExternalIdentityLinkFailureReason.IdentityRejected => LinkProblem(
                StatusCodes.Status401Unauthorized, "Autenticació Google rebutjada", "Google no ha pogut validar aquesta autenticació.", "federated_identity_rejected"),
            ExternalIdentityLinkFailureReason.EmailMismatch => LinkProblem(
                StatusCodes.Status409Conflict, "Compte Google diferent", "El compte Google autenticat no correspon al compte Petiloc actual.", "external_identity_email_mismatch"),
            ExternalIdentityLinkFailureReason.IdentityLinkedToAnotherUser => LinkProblem(
                StatusCodes.Status409Conflict, "Identitat ja vinculada", "Aquesta identitat Google ja està vinculada a un altre compte.", "external_identity_linked_elsewhere"),
            ExternalIdentityLinkFailureReason.ProviderAlreadyLinkedToDifferentIdentity => LinkProblem(
                StatusCodes.Status409Conflict, "Google ja vinculat", "Aquest compte Petiloc ja té una altra identitat Google vinculada.", "provider_already_linked"),
            ExternalIdentityLinkFailureReason.UserNotFound => TypedResults.NotFound(),
            _ => LinkProblem(
                StatusCodes.Status409Conflict, "No s’ha pogut vincular", "La vinculació ha entrat en conflicte. Torna-ho a provar.", "external_identity_link_conflict")
        };
    }

    private static IResult LinkProblem(int status, string title, string detail, string code) =>
        TypedResults.Problem(
            statusCode: status,
            title: title,
            detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });

    private static Results<RedirectHttpResult, NotFound> FacebookStartAsync(
        IAuthApplicationService service,
        string? redirectTo = null)
    {
        var authorizationUrl = service.GetFacebookAuthorizationUrl(redirectTo);
        return string.IsNullOrWhiteSpace(authorizationUrl)
            ? TypedResults.NotFound()
            : TypedResults.Redirect(authorizationUrl);
    }

    private static async Task<RedirectHttpResult> FacebookCallbackAsync(
        IAuthApplicationService service,
        IConfiguration configuration,
        string? code = null,
        string? state = null,
        string? error = null,
        string? error_reason = null,
        CancellationToken cancellationToken = default)
    {
        var frontendBaseUrl = configuration["Auth:FrontendBaseUrl"] ?? "http://localhost:4200";
        var loginUrl = $"{frontendBaseUrl.TrimEnd('/')}/login";

        if (!string.IsNullOrWhiteSpace(error) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return TypedResults.Redirect(
                QueryHelpers.AddQueryString(
                    loginUrl,
                    "federatedError",
                    string.IsNullOrWhiteSpace(error_reason) ? "facebook-login-failed" : error_reason));
        }

        var result = await service.LoginWithFacebookAsync(new FacebookOAuthCallbackRequest(code, state), cancellationToken);
        if (result is null)
        {
            return TypedResults.Redirect(QueryHelpers.AddQueryString(loginUrl, "federatedError", "facebook-login-failed"));
        }

        if (result.Login.ChallengeId is not null)
        {
            var challengeUrl = QueryHelpers.AddQueryString(
                $"{frontendBaseUrl.TrimEnd('/')}/verificar-2fa",
                new Dictionary<string, string?> { ["challenge"] = result.Login.ChallengeId, ["redirectTo"] = result.RedirectTo });
            return TypedResults.Redirect(challengeUrl);
        }

        var serializedSession = JsonSerializer.Serialize(result.Login.Session!, CallbackJsonOptions);
        var sessionPayload = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(serializedSession));
        var callbackUrl = QueryHelpers.AddQueryString(
            $"{frontendBaseUrl.TrimEnd('/')}/auth/callback",
            new Dictionary<string, string?>()
            {
                ["session"] = sessionPayload,
                ["redirectTo"] = result.RedirectTo
            });

        return TypedResults.Redirect(callbackUrl);
    }

    [Authorize]
    private static async Task<Results<Ok<AuthSessionDto>, UnauthorizedHttpResult, NotFound>> GetCurrentSessionAsync(
        ClaimsPrincipal principal,
        IAuthApplicationService service,
        CancellationToken cancellationToken)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(subject, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var session = await service.GetSessionByUserIdAsync(userId, cancellationToken);
        return session is null ? TypedResults.NotFound() : TypedResults.Ok(session);
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(subject, out userId);
    }

}
