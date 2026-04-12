using YepPet.Application.Results;

namespace YepPet.Application.Admin;

public interface IGeographicAdminAppService
{
    Task<IReadOnlyList<CountryAdminDto>> ListCountriesAsync(CancellationToken cancellationToken = default);

    Task<CountryAdminDto?> GetCountryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<CountryAdminDto>> CreateCountryAsync(CreateCountryRequest request, CancellationToken cancellationToken = default);

    Task<Result<CountryAdminDto>> UpdateCountryAsync(Guid id, UpdateCountryRequest request, CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteCountryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CityAdminDto>> ListCitiesAsync(Guid? countryId, CancellationToken cancellationToken = default);

    Task<CityAdminDto?> GetCityAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<CityAdminDto>> CreateCityAsync(CreateCityRequest request, CancellationToken cancellationToken = default);

    Task<Result<CityAdminDto>> UpdateCityAsync(Guid id, UpdateCityRequest request, CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteCityAsync(Guid id, CancellationToken cancellationToken = default);
}
