using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.Places.OpenStreetMap;

internal sealed class GeofabrikRegionalIndex(string directory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    internal bool TryRead(string extractPath, string? sourceVersion, PlaceSourceContext context, out PlaceDiscoveryBatch? batch)
    {
        batch = null;
        var path = IndexPath(context);
        if (!File.Exists(path)) return false;
        try
        {
            var envelope = JsonSerializer.Deserialize<Envelope>(File.ReadAllText(path), Json);
            var file = new FileInfo(extractPath);
            if (envelope is null || envelope.ExtractLength != file.Length || envelope.ExtractLastWriteUtcTicks != file.LastWriteTimeUtc.Ticks
                || envelope.SourceVersion != (sourceVersion ?? string.Empty) || envelope.CategoriesKey != CategoriesKey(context)) return false;
            batch = envelope.Batch; return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException) { return false; }
    }

    internal void Write(string extractPath, string? sourceVersion, PlaceSourceContext context, PlaceDiscoveryBatch batch, TimeSpan buildTime)
    {
        Directory.CreateDirectory(directory);
        var file = new FileInfo(extractPath);
        using var extract = File.OpenRead(extractPath);
        var hash = Convert.ToHexString(SHA256.HashData(extract)).ToLowerInvariant();
        var envelope = new Envelope("catalunya", context.OfficialCode, sourceVersion ?? string.Empty, CategoriesKey(context),
            file.Length, file.LastWriteTimeUtc.Ticks, hash, DateTimeOffset.UtcNow, buildTime.TotalMilliseconds, batch);
        var target = IndexPath(context); var temporary = target + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(envelope, Json)); File.Move(temporary, target, true);
    }

    private string IndexPath(PlaceSourceContext context)
    {
        var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{context.OfficialCode}|{CategoriesKey(context)}"))).ToLowerInvariant()[..16];
        return Path.Combine(directory, $"catalunya-{context.OfficialCode}-{identity}.json");
    }
    private static string CategoriesKey(PlaceSourceContext context) => string.Join(',', context.Categories.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
    internal sealed record Envelope(string RegionKey, string OfficialCode, string SourceVersion, string CategoriesKey,
        long ExtractLength, long ExtractLastWriteUtcTicks, string ExtractSha256, DateTimeOffset CreatedAtUtc,
        double BuildMilliseconds, PlaceDiscoveryBatch Batch);
}
