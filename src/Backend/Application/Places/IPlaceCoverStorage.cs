namespace Zuppeto.Application.Places;

/// <summary>
/// Server-side JPEG cover files for places (path in <c>CoverImageUrl</c>, bytes on disk).
/// </summary>
public sealed record PlaceCoverAttribution(string? AuthorName, string? SourceUri);

public interface IPlaceCoverStorage
{
    Task<string> SaveJpegAsync(
        Guid placeId,
        byte[] jpegBytes,
        PlaceCoverAttribution? attribution,
        CancellationToken cancellationToken = default);

    PlaceCoverAttribution? ReadAttribution(Guid placeId);

    bool HasRecentEnrichmentAttempt(Guid placeId, DateTimeOffset nowUtc, int retentionDays);

    void MarkEnrichmentAttempt(Guid placeId, PlaceCoverAttribution? attribution);

    void Delete(Guid placeId);
}
