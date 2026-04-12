// Copy to: src/Backend/Infrastructure/Persistence/Entities/CountryRecord.cs
namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class CountryRecord
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<CityRecord> Cities { get; set; } = [];
}
