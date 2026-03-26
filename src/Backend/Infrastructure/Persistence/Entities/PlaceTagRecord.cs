namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class PlaceTagRecord
{
    public Guid PlaceId { get; set; }

    public Guid TagId { get; set; }

    public PlaceRecord Place { get; set; } = null!;

    public TagRecord Tag { get; set; } = null!;
}
