namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class TagRecord
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public ICollection<PlaceTagRecord> PlaceTags { get; set; } = [];
}
