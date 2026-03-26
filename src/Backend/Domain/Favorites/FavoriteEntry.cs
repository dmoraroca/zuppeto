using YepPet.Domain.Common;

namespace YepPet.Domain.Favorites;

public sealed class FavoriteEntry : Entity<Guid>
{
    public FavoriteEntry(Guid id, Guid placeId, DateTimeOffset savedAtUtc) : base(id)
    {
        if (placeId == Guid.Empty)
        {
            throw new DomainRuleException("Place id is required.");
        }

        PlaceId = placeId;
        SavedAtUtc = savedAtUtc;
    }

    public Guid PlaceId { get; }

    public DateTimeOffset SavedAtUtc { get; }
}
