using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.GooglePlaces;

/// <summary>Adaptador de consulta de detall; exposa només aquesta capacitat.</summary>
internal sealed class GooglePlacesDetailsAdapter(GooglePlacesApiClient client)
    : IExternalPlaceDetailsProvider
{
    public Task<PlaceExternalDetailsDto?> GetDetailsAsync(
        string externalPlaceId,
        CancellationToken cancellationToken = default) =>
        client.GetDetailsAsync(externalPlaceId, cancellationToken);
}
