using YepPet.Domain.Common;

namespace YepPet.Domain.Favorites;

public sealed class FavoriteList : AggregateRoot<Guid>
{
    private readonly List<FavoriteEntry> _entries = [];

    public FavoriteList(Guid id, Guid ownerUserId) : base(id)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new DomainRuleException("Owner user id is required.");
        }

        OwnerUserId = ownerUserId;
    }

    public Guid OwnerUserId { get; }

    public IReadOnlyCollection<FavoriteEntry> Entries => _entries.AsReadOnly();

    public void AddPlace(Guid placeId, DateTimeOffset savedAtUtc)
    {
        if (_entries.Any(entry => entry.PlaceId == placeId))
        {
            return;
        }

        _entries.Insert(0, new FavoriteEntry(Guid.NewGuid(), placeId, savedAtUtc));
    }

    public void RemovePlace(Guid placeId)
    {
        var existing = _entries.FirstOrDefault(entry => entry.PlaceId == placeId);
        if (existing is null)
        {
            return;
        }

        _entries.Remove(existing);
    }

    public void Clear()
    {
        _entries.Clear();
    }
}
