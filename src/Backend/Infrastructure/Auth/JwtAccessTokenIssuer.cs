using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Zuppeto.Application.Auth;
using Zuppeto.Domain.Users;

namespace Zuppeto.Infrastructure.Auth;

internal sealed class JwtAccessTokenIssuer(IOptions<AuthOptions> options) : IAccessTokenIssuer
{
    public AccessTokenResult Issue(User user)
    {
        var jwtOptions = options.Value.Jwt;
        var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.ExpiresInMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Name, user.Profile.DisplayName),
                new Claim("security_version", user.SecurityVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))
            ],
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAtUtc);
    }
}
