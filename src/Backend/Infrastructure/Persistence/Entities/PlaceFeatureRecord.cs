namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class PlaceFeatureRecord
{
    public Guid PlaceId { get; set; }

    public Guid FeatureId { get; set; }

    public PlaceRecord Place { get; set; } = null!;

    public FeatureRecord Feature { get; set; } = null!;
}
