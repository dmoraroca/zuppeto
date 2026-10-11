namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class TerritorialSourceApprovalAuditRecord
{
    public Guid Id { get; set; }
    public Guid DatasetSourceId { get; set; }
    public TerritorialDatasetSourceRecord DatasetSource { get; set; } = null!;
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public UserRecord ActorUser { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
