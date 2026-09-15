namespace Zuppeto.Infrastructure.Persistence.Entities;
public sealed class TotpRecoveryCodeRecord { public Guid Id { get; set; } public Guid UserId { get; set; } public string CodeHash { get; set; } = string.Empty; public DateTimeOffset CreatedAtUtc { get; set; } public DateTimeOffset? UsedAtUtc { get; set; } }
