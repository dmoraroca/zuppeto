namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class PlaceSearchQueryRecord
{
    public Guid Id { get; set; }

    public string QueryKey { get; set; } = string.Empty;

    public string SearchText { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string PetCategory { get; set; } = string.Empty;

    public int HitCount { get; set; }

    public int ResultCount { get; set; }

    public DateTimeOffset LastRunAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<PlaceSearchQueryResultRecord> Results { get; set; } = [];
}
