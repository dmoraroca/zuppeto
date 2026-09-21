namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class TerritorialUnitCodeRecord
{
    public Guid Id { get; set; }
    public Guid TerritorialUnitId { get; set; }
    public TerritorialUnitRecord TerritorialUnit { get; set; } = null!;
    public string Scheme { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsPrimary { get; set; }
    public Guid? DatasetSourceId { get; set; }
    public TerritorialDatasetSourceRecord? DatasetSource { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
