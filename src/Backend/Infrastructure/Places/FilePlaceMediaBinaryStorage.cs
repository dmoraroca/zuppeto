using Microsoft.Extensions.Hosting;
using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.Places;

internal sealed class FilePlaceMediaBinaryStorage : IPlaceMediaBinaryStorage
{
    private readonly string directory;

    public FilePlaceMediaBinaryStorage(IHostEnvironment hostEnvironment)
    {
        directory = Path.Combine(hostEnvironment.ContentRootPath, "storage", "place-media");
        Directory.CreateDirectory(directory);
    }

    public async Task<string> SaveJpegAsync(
        Guid placeId,
        Guid assetId,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        if (content.Length < 4 || content[0] != 0xff || content[1] != 0xd8)
            throw new InvalidOperationException("El contingut multimèdia no és un JPEG vàlid.");
        var fileName = FileName(placeId, assetId);
        await File.WriteAllBytesAsync(Path.Combine(directory, fileName), content, cancellationToken);
        return $"/media/place-media/{fileName}";
    }

    public void Delete(Guid placeId, Guid assetId)
    {
        var path = Path.Combine(directory, FileName(placeId, assetId));
        if (File.Exists(path)) File.Delete(path);
    }

    private static string FileName(Guid placeId, Guid assetId) => $"{placeId:N}-{assetId:N}.jpg";
}
