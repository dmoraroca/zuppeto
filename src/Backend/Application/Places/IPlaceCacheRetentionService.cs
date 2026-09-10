namespace Zuppeto.Application.Places;

public sealed record PlaceCacheRetentionResult(
    int DeletedSearchSnapshots,
    int RedactedCoordinateCaches);

/// <summary>Port de manteniment de la cache; la infraestructura decideix com persistir-la.</summary>
public interface IPlaceCacheRetentionService
{
    Task<PlaceCacheRetentionResult> ExpireAsync(
        DateTimeOffset nowUtc,
        bool redactExpiredCoordinates,
        CancellationToken cancellationToken = default);
}
