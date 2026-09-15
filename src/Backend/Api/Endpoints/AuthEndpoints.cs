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
        group.MapPost("/activation", ActivateEmailAsync);
        group.MapPost("/activation/resend", ResendActivationEmailAsync);
        group.MapPost("/google", GoogleLoginAsync);
        group.MapGet("/linkedin/start", LinkedInStartAsync);
        group.MapGet("/linkedin/callback", LinkedInCallbackAsync);
        group.MapGet("/facebook/start", FacebookStartAsync);
        group.MapGet("/facebook/callback", FacebookCallbackAsync);
        group.MapGet("/providers", GetProviders);
        group.MapGet("/me", GetCurrentSessionAsync).RequireAuthorization();

        return app;
    }

    private static async Task<Results<Ok<AuthSessionDto>, UnauthorizedHttpResult, ProblemHttpResult, ValidationProblem>> LoginAsync(
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
        return result.FailureReason == LoginFailureReason.EmailActivationRequired
            ? TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Activació de compte necessària", detail: "Activa el compte des del correu abans d'iniciar sessió.")
            : TypedResults.Unauthorized();
    }

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

    private static async Task<Results<Ok<AuthSessionDto>, UnauthorizedHttpResult, ValidationProblem>> GoogleLoginAsync(
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

        var session = await service.LoginWithGoogleAsync(request, cancellationToken);
        return session is null ? TypedResults.Unauthorized() : TypedResults.Ok(session);
    }

    private static Ok<IReadOnlyCollection<AuthProviderDto>> GetProviders(IAuthApplicationService service)
    {
        return TypedResults.Ok(service.GetProviders());
    }

    private static Results<RedirectHttpResult, NotFound> LinkedInStartAsync(
        IAuthApplicationService service,
        string? redirectTo = null)
    {
        var authorizationUrl = service.GetLinkedInAuthorizationUrl(redirectTo);
        return string.IsNullOrWhiteSpace(authorizationUrl)
            ? TypedResults.NotFound()
            : TypedResults.Redirect(authorizationUrl);
    }

    private static async Task<RedirectHttpResult> LinkedInCallbackAsync(
        IAuthApplicationService service,
        IConfiguration configuration,
        string? code = null,
        string? state = null,
        string? error = null,
        string? error_description = null,
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
                    string.IsNullOrWhiteSpace(error_description) ? "linkedin-login-failed" : error_description));
        }

        var result = await service.LoginWithLinkedInAsync(new LinkedInOAuthCallbackRequest(code, state), cancellationToken);
        if (result is null)
        {
            return TypedResults.Redirect(QueryHelpers.AddQueryString(loginUrl, "federatedError", "linkedin-login-failed"));
        }

        var serializedSession = JsonSerializer.Serialize(result.Session, CallbackJsonOptions);
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

        var serializedSession = JsonSerializer.Serialize(result.Session, CallbackJsonOptions);
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
}
