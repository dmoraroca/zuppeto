using Zuppeto.Domain.Places;

namespace Zuppeto.Application.Places;

/// <summary>Comandament explícit que actualitza únicament camps governats externament.</summary>
public interface IPlaceExternalDataSynchronizer
{
    Task<Place?> SynchronizeAsync(
        Guid placeId,
        CancellationToken cancellationToken = default);
}
