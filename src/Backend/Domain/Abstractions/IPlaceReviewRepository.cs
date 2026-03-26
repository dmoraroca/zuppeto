using YepPet.Domain.Reviews;

namespace YepPet.Domain.Abstractions;

public interface IPlaceReviewRepository
{
    Task<PlaceReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PlaceReview?> GetByAuthorAndPlaceAsync(
        Guid authorUserId,
        Guid placeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PlaceReview>> GetByPlaceAsync(
        Guid placeId,
        PlaceReviewQuery query,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlaceReview review, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlaceReview review, CancellationToken cancellationToken = default);
}
