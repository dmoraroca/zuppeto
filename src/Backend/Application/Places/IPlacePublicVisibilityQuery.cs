namespace Zuppeto.Application.Places;

public interface IPlacePublicVisibilityQuery
{
    Task<bool> IsVisibleAsync(Guid placeId, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Guid>> FilterVisibleAsync(
        IReadOnlyCollection<Guid> placeIds,
        CancellationToken cancellationToken = default);
}
