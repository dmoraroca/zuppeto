namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class TerritorialUnitNameRecord
{
    public Guid Id { get; set; }
    public Guid TerritorialUnitId { get; set; }
    public TerritorialUnitRecord TerritorialUnit { get; set; } = null!;
    public string? Locale { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string NormalizedName { get; set; } = string.Empty;
    public Guid? DatasetSourceId { get; set; }
    public TerritorialDatasetSourceRecord? DatasetSource { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
