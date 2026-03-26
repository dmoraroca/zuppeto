namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class FavoriteListRecord
{
    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public UserRecord OwnerUser { get; set; } = null!;

    public ICollection<FavoriteEntryRecord> Entries { get; set; } = [];
}
