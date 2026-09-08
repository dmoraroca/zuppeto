using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zuppeto.Infrastructure.Persistence;

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
                var db = scope.ServiceProvider.GetRequiredService<ZuppetoDbContext>();
                var now = DateTimeOffset.UtcNow;

                var deletedQueries = await db.Database.ExecuteSqlInterpolatedAsync(
                    $"""DELETE FROM place_search_queries WHERE expires_at_utc < {now};""",
                    stoppingToken);

                if (deletedQueries > 0)
                {
                    logger.LogInformation(
                        "Purged {Count} expired place search query snapshot(s).",
                        deletedQueries);
                }

                if (options.Value.Enabled)
                {
                    var redacted = await db.Database.ExecuteSqlInterpolatedAsync(
                        $"""
                         UPDATE places
                         SET latitude = NULL,
                             longitude = NULL,
                             exclude_from_osm_map = TRUE,
                             google_coordinates_cached_until = NULL,
                             last_google_sync_at = NULL
                         WHERE data_provenance IN ('GooglePlaces', 'Mixed')
                           AND google_coordinates_cached_until IS NOT NULL
                           AND google_coordinates_cached_until < {now};
                         """,
                        stoppingToken);

                    if (redacted > 0)
                    {
                        logger.LogInformation(
                            "Redacted expired Google Places coordinate cache on {Count} place row(s).",
                            redacted);
                    }
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
