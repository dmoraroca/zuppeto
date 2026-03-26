using YepPet.Domain.Places;

namespace YepPet.Domain.Abstractions;

public interface IPlaceRepository
{
    Task<Place?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Place>> SearchAsync(
        PlaceSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Place>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetAvailableCitiesAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Place place, CancellationToken cancellationToken = default);

    Task UpdateAsync(Place place, CancellationToken cancellationToken = default);
}
