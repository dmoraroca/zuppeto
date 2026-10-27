using Microsoft.Extensions.Configuration;

namespace Zuppeto.Infrastructure.Auth;

public static class AuthOptionsFactory
{
    private const int MinimumSigningKeyLength = 32;

    public static AuthOptions Create(IConfiguration configuration)
    {
        var options = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

        if (string.IsNullOrWhiteSpace(options.Jwt.SigningKey) ||
            options.Jwt.SigningKey.Length < MinimumSigningKeyLength)
        {
            throw new InvalidOperationException(
                "Falta una clau JWT segura. Configura Auth:Jwt:SigningKey fora de Git.");
        }

        return options;
    }
}
