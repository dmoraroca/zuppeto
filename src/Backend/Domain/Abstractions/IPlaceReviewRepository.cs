using YepPet.Domain.Reviews;

namespace YepPet.Domain.Abstractions;

public interface IPlaceReviewRepository
{
    Task<PlaceReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PlaceReview>> GetVisibleByPlaceAsync(Guid placeId, CancellationToken cancellationToken = default);

    Task AddAsync(PlaceReview review, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlaceReview review, CancellationToken cancellationToken = default);
}
