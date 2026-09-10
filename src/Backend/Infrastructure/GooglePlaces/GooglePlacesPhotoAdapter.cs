using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.GooglePlaces;

/// <summary>Adaptador de fotografies; exposa només la descàrrega binària.</summary>
internal sealed class GooglePlacesPhotoAdapter(GooglePlacesApiClient client)
    : IExternalPlacePhotoProvider
{
    public Task<byte[]?> DownloadPhotoAsync(
        string photoReferenceOrName,
        CancellationToken cancellationToken = default) =>
        client.DownloadPhotoAsync(photoReferenceOrName, cancellationToken);
}
