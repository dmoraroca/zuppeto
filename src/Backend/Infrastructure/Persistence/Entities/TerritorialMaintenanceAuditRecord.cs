namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class TerritorialMaintenanceAuditRecord
{
    public Guid Id { get; set; }
    public Guid TerritorialUnitId { get; set; }
    public TerritorialUnitRecord TerritorialUnit { get; set; } = null!;
    public string Action { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string? BeforeValue { get; set; }
    public string? AfterValue { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public UserRecord ActorUser { get; set; } = null!;
    public string Origin { get; set; } = "Manual";
    public DateTimeOffset CreatedAtUtc { get; set; }
}
