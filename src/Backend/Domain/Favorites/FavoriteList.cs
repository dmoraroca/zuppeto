using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Favorites;

public sealed class FavoriteList : AggregateRoot<Guid>
{
    private readonly List<FavoriteEntry> _entries = [];

    public FavoriteList(Guid id, Guid ownerUserId) : base(id)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new DomainRuleException("El propietari és obligatori.");
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

    /// <summary>
    /// Rehydrates an entry with its persisted id. Used by infrastructure mapping only;
    /// <see cref="AddPlace"/> must not be used for load because it would mint new ids and break EF sync.
    /// </summary>
    public void RestorePersistedEntry(Guid entryId, Guid placeId, DateTimeOffset savedAtUtc)
    {
        if (entryId == Guid.Empty)
        {
            throw new DomainRuleException("L’entrada és obligatòria.");
        }

        if (_entries.Any(entry => entry.Id == entryId || entry.PlaceId == placeId))
        {
            return;
        }

        _entries.Add(new FavoriteEntry(entryId, placeId, savedAtUtc));
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
