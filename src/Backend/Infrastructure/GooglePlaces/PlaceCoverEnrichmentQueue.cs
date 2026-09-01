using System.Collections.Concurrent;
using System.Threading.Channels;
using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.GooglePlaces;

internal sealed class PlaceCoverEnrichmentQueue : IPlaceCoverEnrichmentQueue
{
    private readonly ConcurrentDictionary<Guid, byte> queued = new();
    private readonly Channel<Guid> channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<Guid> Reader => channel.Reader;

    public void Enqueue(IReadOnlyCollection<Guid> placeIds)
    {
        foreach (var placeId in placeIds)
        {
            if (placeId == Guid.Empty || !queued.TryAdd(placeId, 0))
            {
                continue;
            }

            if (!channel.Writer.TryWrite(placeId))
            {
                queued.TryRemove(placeId, out _);
            }
        }
    }

    public void MarkProcessed(Guid placeId) => queued.TryRemove(placeId, out _);
}
