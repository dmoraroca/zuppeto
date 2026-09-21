namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class TerritorialLocaleAssignmentRecord
{
    public Guid Id { get; set; }
    public Guid CountryId { get; set; }
    public CountryRecord Country { get; set; } = null!;
    public Guid? TerritorialUnitId { get; set; }
    public TerritorialUnitRecord? TerritorialUnit { get; set; }
    public string Locale { get; set; } = string.Empty;
    public bool IsOfficial { get; set; }
    public int Priority { get; set; }
    public Guid? DatasetSourceId { get; set; }
    public TerritorialDatasetSourceRecord? DatasetSource { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
