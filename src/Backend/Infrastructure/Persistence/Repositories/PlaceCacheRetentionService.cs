using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Places;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

/// <summary>Adaptador EF de retenció; concentra les operacions massives de persistència.</summary>
internal sealed class PlaceCacheRetentionService(ZuppetoDbContext dbContext)
    : IPlaceCacheRetentionService
{
    public async Task<PlaceCacheRetentionResult> ExpireAsync(
        DateTimeOffset nowUtc,
        bool redactExpiredCoordinates,
        CancellationToken cancellationToken = default)
    {
        var deletedQueries = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""DELETE FROM place_search_queries WHERE expires_at_utc < {nowUtc};""",
            cancellationToken);

        if (!redactExpiredCoordinates)
        {
            return new PlaceCacheRetentionResult(deletedQueries, 0);
        }

        var coordinateFlag = (int)PlaceManualFields.Coordinates;
        var redacted = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE places
             SET latitude = NULL,
                 longitude = NULL,
                 exclude_from_osm_map = TRUE,
                 google_coordinates_cached_until = NULL,
                 last_google_sync_at = NULL
             WHERE data_provenance IN ('GooglePlaces', 'Mixed')
               AND (manual_fields & {coordinateFlag}) = 0
               AND google_coordinates_cached_until IS NOT NULL
               AND google_coordinates_cached_until < {nowUtc};
             """,
            cancellationToken);

        return new PlaceCacheRetentionResult(deletedQueries, redacted);
    }
}
