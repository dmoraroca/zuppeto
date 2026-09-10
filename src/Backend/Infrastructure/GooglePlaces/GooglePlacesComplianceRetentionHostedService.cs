using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.GooglePlaces;

/// <summary>
/// Periodically removes expired internal search snapshots and redacts expired Google-sourced coordinate caches.
/// </summary>
internal sealed class GooglePlacesComplianceRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<GooglePlacesComplianceOptions> options,
    ILogger<GooglePlacesComplianceRetentionHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var now = DateTimeOffset.UtcNow;
                var retentionService = scope.ServiceProvider.GetRequiredService<IPlaceCacheRetentionService>();
                var result = await retentionService.ExpireAsync(
                    now,
                    options.Value.Enabled,
                    stoppingToken);

                if (result.DeletedSearchSnapshots > 0)
                {
                    logger.LogInformation(
                        "Purged {Count} expired place search query snapshot(s).",
                        result.DeletedSearchSnapshots);
                }

                if (result.RedactedCoordinateCaches > 0)
                {
                    logger.LogInformation(
                        "Redacted expired Google Places coordinate cache on {Count} place row(s).",
                        result.RedactedCoordinateCaches);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Google Places compliance retention run failed.");
            }

            var delayMinutes = Math.Max(5, options.Value.RunIntervalMinutes);
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
