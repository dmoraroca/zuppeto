namespace YepPet.Application.Places;

/// <summary>
/// External source for place discovery candidates (e.g., Google Places).
/// </summary>
public interface IExternalPlaceSuggestionProvider
{
    Task<IReadOnlyCollection<PlaceExternalCandidateDto>> SearchPlacesAsync(
        PlaceExternalSearchRequest request,
        CancellationToken cancellationToken = default);
}
