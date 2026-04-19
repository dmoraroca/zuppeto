namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class CityRecord
{
    public Guid Id { get; set; }

    public Guid CountryId { get; set; }

    public CountryRecord Country { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
