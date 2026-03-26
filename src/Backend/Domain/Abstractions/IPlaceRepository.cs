using YepPet.Domain.Places;

namespace YepPet.Domain.Abstractions;

public interface IPlaceRepository
{
    Task<Place?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Place>> SearchAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Place place, CancellationToken cancellationToken = default);

    Task UpdateAsync(Place place, CancellationToken cancellationToken = default);
}
