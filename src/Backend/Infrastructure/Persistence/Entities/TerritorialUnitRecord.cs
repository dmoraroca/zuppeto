namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class TerritorialUnitRecord
{
    public Guid Id { get; set; }
    public Guid CountryId { get; set; }
    public CountryRecord Country { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public TerritorialUnitRecord? Parent { get; set; }
    public ICollection<TerritorialUnitRecord> Children { get; set; } = [];
    public Guid TerritorialUnitTypeId { get; set; }
    public TerritorialUnitTypeRecord TerritorialUnitType { get; set; } = null!;
    public bool IsActive { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public Guid? CoordinateSourceId { get; set; }
    public TerritorialDatasetSourceRecord? CoordinateSource { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<TerritorialUnitNameRecord> Names { get; set; } = [];
    public ICollection<TerritorialUnitCodeRecord> Codes { get; set; } = [];
    public ICollection<TerritorialLocaleAssignmentRecord> LocaleAssignments { get; set; } = [];
}
