using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.GooglePlaces;

/// <summary>
/// Fills missing Google covers in the background so GET /api/places can return the catalog immediately.
/// </summary>
internal sealed class PlaceCoverEnrichmentHostedService(
    PlaceCoverEnrichmentQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<PlaceCoverEnrichmentHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var placeId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var places = scope.ServiceProvider.GetRequiredService<IPlaceApplicationService>();
                await places.GetByIdAsync(placeId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Background cover enrichment failed for {PlaceId}.", placeId);
            }
            finally
            {
                queue.MarkProcessed(placeId);
            }
        }
    }
}
