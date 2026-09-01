using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.GooglePlaces;

internal sealed class FilePlaceCoverStorage : IPlaceCoverStorage
{
    private readonly string directory;

    public FilePlaceCoverStorage(IHostEnvironment hostEnvironment)
    {
        directory = Path.Combine(hostEnvironment.ContentRootPath, "storage", "place-covers");
        Directory.CreateDirectory(directory);
    }

    public async Task<string> SaveJpegAsync(
        Guid placeId,
        byte[] jpegBytes,
        PlaceCoverAttribution? attribution,
        CancellationToken cancellationToken = default)
    {
        var imagePath = Path.Combine(directory, FileName(placeId, ".jpg"));
        await File.WriteAllBytesAsync(imagePath, jpegBytes, cancellationToken);

        var metaPath = Path.Combine(directory, FileName(placeId, ".json"));
        var json = JsonSerializer.Serialize(attribution ?? new PlaceCoverAttribution(null, null));
        await File.WriteAllTextAsync(metaPath, json, cancellationToken);

        return $"/media/place-covers/{placeId:N}.jpg";
    }

    public PlaceCoverAttribution? ReadAttribution(Guid placeId)
    {
        var metaPath = Path.Combine(directory, FileName(placeId, ".json"));
        if (!File.Exists(metaPath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PlaceCoverAttribution>(File.ReadAllText(metaPath));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public bool HasRecentEnrichmentAttempt(Guid placeId, DateTimeOffset nowUtc, int retentionDays)
    {
        var imagePath = Path.Combine(directory, FileName(placeId, ".jpg"));
        if (!File.Exists(imagePath))
        {
            return false;
        }

        var latest = LatestWriteTimeUtc(placeId);
        if (latest is null)
        {
            return false;
        }

        var window = TimeSpan.FromDays(Math.Clamp(retentionDays, 1, 366));
        return nowUtc - latest.Value < window;
    }

    public void MarkEnrichmentAttempt(Guid placeId, PlaceCoverAttribution? attribution)
    {
        var metaPath = Path.Combine(directory, FileName(placeId, ".json"));
        var json = JsonSerializer.Serialize(attribution ?? new PlaceCoverAttribution(null, null));
        File.WriteAllText(metaPath, json);
    }

    public void Delete(Guid placeId)
    {
        var imagePath = Path.Combine(directory, FileName(placeId, ".jpg"));
        var metaPath = Path.Combine(directory, FileName(placeId, ".json"));
        if (File.Exists(imagePath))
        {
            File.Delete(imagePath);
        }

        if (File.Exists(metaPath))
        {
            File.Delete(metaPath);
        }
    }

    private DateTimeOffset? LatestWriteTimeUtc(Guid placeId)
    {
        DateTimeOffset? latest = null;
        foreach (var extension in new[] { ".jpg", ".json" })
        {
            var path = Path.Combine(directory, FileName(placeId, extension));
            if (!File.Exists(path))
            {
                continue;
            }

            var write = File.GetLastWriteTimeUtc(path);
            var stamp = new DateTimeOffset(write, TimeSpan.Zero);
            if (latest is null || stamp > latest)
            {
                latest = stamp;
            }
        }

        return latest;
    }

    private static string FileName(Guid placeId, string extension) => $"{placeId:N}{extension}";
}
