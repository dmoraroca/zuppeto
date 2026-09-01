namespace Zuppeto.Application.Places;

/// <summary>
/// Queues catalog rows for Place Details / Place Photos without blocking the listing HTTP response.
/// </summary>
public interface IPlaceCoverEnrichmentQueue
{
    void Enqueue(IReadOnlyCollection<Guid> placeIds);
}
