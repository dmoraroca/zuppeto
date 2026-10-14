using Microsoft.Extensions.Options;
using Zuppeto.Application.Places;
using Zuppeto.Application.Results;

namespace Zuppeto.Infrastructure.Places.OpenStreetMap;

internal sealed class OverpassPolicyDiscoveryDecorator(
    OverpassPlaceDiscoverySource inner,
    IOptions<OverpassOptions> options) : IPlaceDiscoverySource
{
    private static readonly SemaphoreSlim ConcurrencyGate = new(1, 1);
    private readonly OverpassOptions settings = options.Value;
    public PlaceSourceDescriptor Descriptor => inner.Descriptor;

    public async Task<Result<PlaceDiscoveryBatch>> DiscoverAsync(PlaceSourceContext context, CancellationToken cancellationToken)
    {
        await ConcurrencyGate.WaitAsync(cancellationToken);
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 5, 60)));
                try
                {
                    var result = await inner.DiscoverAsync(context, timeout.Token);
                    if (result.IsSuccess || result.Failure?.Kind is not (FailureKind.ExternalUnavailable or FailureKind.RateLimited)
                        || attempt >= Math.Clamp(settings.MaximumRetries, 0, 2)) return result;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    if (attempt >= Math.Clamp(settings.MaximumRetries, 0, 2))
                        return Result<PlaceDiscoveryBatch>.Fail(FailureKind.ExternalUnavailable, "La consulta Overpass ha excedit el temps màxim.");
                }
                await Task.Delay(TimeSpan.FromMilliseconds(settings.BackoffMilliseconds * (attempt + 1)), cancellationToken);
            }
        }
        finally { ConcurrencyGate.Release(); }
    }
}
