namespace YepPet.Application.Places;

public interface IPlaceApplicationService
{
    Task<PlaceDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PlaceSummaryDto>> SearchAsync(
        PlaceSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PlaceSearchHistoryDto>> GetRecentSearchesAsync(
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetAvailableCitiesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PlaceCitySuggestionDto>> SearchAvailableCitiesAsync(
        PlaceCitySearchRequest request,
        CancellationToken cancellationToken = default);

    Task<Guid> SaveAsync(PlaceUpsertRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
