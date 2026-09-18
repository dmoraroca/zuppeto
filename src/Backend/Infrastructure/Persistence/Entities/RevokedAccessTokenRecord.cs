namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class RevokedAccessTokenRecord
{
    public string TokenId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset RevokedAtUtc { get; set; }
}
