namespace YepPet.Domain.Abstractions;

public interface IPlaceSearchQueryRepository
{
    public sealed record SearchSnapshotKey(
        string SearchText,
        string City,
        string Type,
        string PetCategory);

    Task<IReadOnlyList<Guid>?> TryGetFreshPlaceIdsAsync(
        SearchSnapshotKey key,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task SaveSnapshotAsync(
        SearchSnapshotKey key,
        IReadOnlyList<Guid> orderedPlaceIds,
        DateTimeOffset nowUtc,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PlaceSearchHistoryRow>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken = default);

    public sealed record PlaceSearchHistoryRow(
        string SearchText,
        string City,
        string Type,
        string PetCategory,
        int HitCount,
        int ResultCount,
        DateTimeOffset LastRunAtUtc);
}
