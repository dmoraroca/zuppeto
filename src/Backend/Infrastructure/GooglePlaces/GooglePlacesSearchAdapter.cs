using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.GooglePlaces;

/// <summary>Adaptador de descobriment; exposa només la capacitat de cerca.</summary>
internal sealed class GooglePlacesSearchAdapter(GooglePlacesApiClient client)
    : IExternalPlaceSuggestionProvider
{
    public Task<IReadOnlyCollection<PlaceExternalCandidateDto>> SearchPlacesAsync(
        PlaceExternalSearchRequest request,
        CancellationToken cancellationToken = default) =>
        client.SearchPlacesAsync(request, cancellationToken);
}
