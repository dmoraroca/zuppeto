namespace Zuppeto.Infrastructure.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public JwtOptions Jwt { get; init; } = new();
    public string FrontendBaseUrl { get; init; } = "http://localhost:4200";
    public GoogleOptions Google { get; init; } = new();
    public FacebookOptions Facebook { get; init; } = new();

    public sealed class JwtOptions
    {
        public string Issuer { get; init; } = "Zuppeto";

        public string Audience { get; init; } = "Zuppeto.Web";

        public string SigningKey { get; init; } = "dev-only-zuppeto-signing-key-change-in-production-123456789";

        public int ExpiresInMinutes { get; init; } = 480;
    }

    public sealed class GoogleOptions
    {
        public string ClientId { get; init; } = string.Empty;

        public string[] AdminEmails { get; init; } = [];
    }

    public sealed class FacebookOptions
    {
        public string AppId { get; init; } = string.Empty;

        public string AppSecret { get; init; } = string.Empty;

        public string RedirectUri { get; init; } = string.Empty;

        public string[] AdminEmails { get; init; } = [];
    }
}
