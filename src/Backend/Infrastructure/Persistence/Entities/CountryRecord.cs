namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class CountryRecord
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Iso2 { get; set; }

    public string? Iso3 { get; set; }

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<CityRecord> Cities { get; set; } = [];

    public ICollection<TerritorialUnitTypeRecord> TerritorialUnitTypes { get; set; } = [];

    public ICollection<TerritorialUnitRecord> TerritorialUnits { get; set; } = [];

    public ICollection<TerritorialLocaleAssignmentRecord> TerritorialLocaleAssignments { get; set; } = [];

    public ICollection<TerritorialDatasetSourceRecord> TerritorialDatasetSources { get; set; } = [];
}
