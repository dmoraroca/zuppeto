namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class FeatureRecord
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public ICollection<PlaceFeatureRecord> PlaceFeatures { get; set; } = [];
}
