using YepPet.Domain.Geography;

namespace YepPet.Domain.Abstractions;

public interface IGeographicCatalogRepository
{
    Task<IReadOnlyList<CountryRow>> ListCountriesAsync(CancellationToken cancellationToken = default);

    Task<CountryRow?> GetCountryByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> IsCountryCodeInUseAsync(string normalizedCode, Guid? exceptCountryId, CancellationToken cancellationToken = default);

    Task<bool> IsCityNormalizedNameInUseAsync(
        Guid countryId,
        string normalizedName,
        Guid? exceptCityId,
        CancellationToken cancellationToken = default);

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

    Task<bool> CountryHasCitiesAsync(Guid countryId, CancellationToken cancellationToken = default);

    Task<bool> DeleteCountryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CityRow>> ListCitiesAsync(Guid? countryId, CancellationToken cancellationToken = default);

    Task<CityRow?> GetCityByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CityRow> CreateCityAsync(
        Guid countryId,
        string name,
        decimal? latitude,
        decimal? longitude,
        bool isActive,
        int sortOrder,
        CancellationToken cancellationToken = default);

    Task<CityRow?> UpdateCityAsync(
        Guid id,
        string name,
        decimal? latitude,
        decimal? longitude,
        bool isActive,
        int sortOrder,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteCityAsync(Guid id, CancellationToken cancellationToken = default);
}
