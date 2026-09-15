namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class ExternalIdentityRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public UserRecord? User { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTimeOffset LinkedAtUtc { get; set; }
}
