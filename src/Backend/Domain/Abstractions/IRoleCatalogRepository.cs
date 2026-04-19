using YepPet.Domain.Roles;

namespace YepPet.Domain.Abstractions;

public interface IRoleCatalogRepository
{
    Task<IReadOnlyList<RoleCatalogRow>> ListAsync(CancellationToken cancellationToken = default);

    Task<RoleCatalogRow?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken = default);

    Task<RoleCatalogRow> CreateAsync(string key, string displayName, CancellationToken cancellationToken = default);

    Task<RoleCatalogRow?> UpdateAsync(
        string key,
        string displayName,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<RoleDeleteOutcome> TryDeleteAsync(string key, CancellationToken cancellationToken = default);
}
