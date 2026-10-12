using Zuppeto.Application.Results;

namespace Zuppeto.Application.Admin;

public interface IGeographicAdminAppService
{
    Task<IReadOnlyList<CountryAdminDto>> ListCountriesAsync(CancellationToken cancellationToken = default);

    Task<CountryAdminDto?> GetCountryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<CountryAdminDto>> CreateCountryAsync(CreateCountryRequest request, CancellationToken cancellationToken = default);

    Task<Result<CountryAdminDto>> UpdateCountryAsync(Guid id, UpdateCountryRequest request, CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteCountryAsync(Guid id, CancellationToken cancellationToken = default);
}
