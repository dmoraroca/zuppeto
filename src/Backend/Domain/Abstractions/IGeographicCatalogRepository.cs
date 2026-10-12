using Zuppeto.Domain.Geography;

namespace Zuppeto.Domain.Abstractions;

public interface IGeographicCatalogRepository
{
    Task<IReadOnlyList<CountryRow>> ListCountriesAsync(CancellationToken cancellationToken = default);

    Task<CountryRow?> GetCountryByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> IsCountryCodeInUseAsync(string normalizedCode, Guid? exceptCountryId, CancellationToken cancellationToken = default);

    Task<CountryRow> CreateCountryAsync(
        string code,
        string name,
        bool isActive,
        int sortOrder,
        CancellationToken cancellationToken = default);

    Task<CountryRow?> UpdateCountryAsync(
        Guid id,
        string code,
        string name,
        bool isActive,
        int sortOrder,
        CancellationToken cancellationToken = default);

    Task<bool> CountryHasTerritorialDependenciesAsync(Guid countryId, CancellationToken cancellationToken = default);

    Task<bool> DeleteCountryAsync(Guid id, CancellationToken cancellationToken = default);
}
