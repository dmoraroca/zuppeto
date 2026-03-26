namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class PrivacyConsentEventRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public bool Accepted { get; set; }

    public DateTimeOffset RegisteredAtUtc { get; set; }

    public string Source { get; set; } = string.Empty;

    public UserRecord User { get; set; } = null!;
}
