namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class FavoriteEntryRecord
{
    public Guid Id { get; set; }

    public Guid FavoriteListId { get; set; }

    public Guid PlaceId { get; set; }

    public DateTimeOffset SavedAtUtc { get; set; }

    public FavoriteListRecord FavoriteList { get; set; } = null!;

    public PlaceRecord Place { get; set; } = null!;
}
