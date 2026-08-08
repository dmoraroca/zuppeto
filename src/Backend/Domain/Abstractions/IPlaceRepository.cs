using Zuppeto.Domain.Places;

namespace Zuppeto.Domain.Abstractions;

public interface IPlaceRepository
{
    public sealed record CityCatalogItem(string City, string Country);

    Task<Place?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Place?> GetByGooglePlaceIdAsync(string googlePlaceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Place>> SearchAsync(
        PlaceSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Place>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetAvailableCitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Distinct city names from places whose city contains the fragment (case-insensitive).
    /// </summary>
    Task<IReadOnlyCollection<CityCatalogItem>> SearchAvailableCitiesAsync(
        string normalizedQueryFragment,
        int limit,
        CancellationToken cancellationToken = default);

    Task AddAsync(Place place, CancellationToken cancellationToken = default);

    Task UpdateAsync(Place place, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
