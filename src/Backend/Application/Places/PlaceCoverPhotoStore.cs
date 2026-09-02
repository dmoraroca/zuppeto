namespace Zuppeto.Application.Places;

internal sealed class PlaceCoverPhotoStore(
    IExternalPlaceDetailsProvider detailsProvider,
    IPlaceCoverStorage coverStorage)
{
    internal async Task<string?> TryStoreFromReferenceAsync(
        Guid placeId,
        string photoReference,
        PlaceCoverAttribution? attribution,
        CancellationToken cancellationToken)
    {
        var bytes = await detailsProvider.DownloadPhotoAsync(photoReference, cancellationToken);
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        return await coverStorage.SaveJpegAsync(placeId, bytes, attribution, cancellationToken);
    }
}
