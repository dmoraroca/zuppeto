namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class TerritorialUnitTypeRecord
{
    public Guid Id { get; set; }
    public Guid CountryId { get; set; }
    public CountryRecord Country { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsSelectableLocality { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<TerritorialUnitRecord> TerritorialUnits { get; set; } = [];
}
