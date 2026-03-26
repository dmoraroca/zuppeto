using YepPet.Domain.Favorites;

namespace YepPet.Domain.Abstractions;

public interface IFavoriteListRepository
{
    Task<FavoriteList?> GetByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);

    Task AddAsync(FavoriteList favoriteList, CancellationToken cancellationToken = default);

    Task UpdateAsync(FavoriteList favoriteList, CancellationToken cancellationToken = default);
}
