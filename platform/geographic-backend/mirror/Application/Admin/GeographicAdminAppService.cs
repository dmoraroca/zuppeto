using Zuppeto.Application.Results;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Geography;

namespace Zuppeto.Application.Admin;

public sealed class GeographicAdminAppService(IGeographicCatalogRepository repository) : IGeographicAdminAppService
{
    public async Task<IReadOnlyList<CountryAdminDto>> ListCountriesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await repository.ListCountriesAsync(cancellationToken);
        return rows.Select(ToDto).ToArray();
    }

    public async Task<CountryAdminDto?> GetCountryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await repository.GetCountryByIdAsync(id, cancellationToken);
        return row is null ? null : ToDto(row);
    }

    public async Task<Result<CountryAdminDto>> CreateCountryAsync(CreateCountryRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await repository.IsCountryCodeInUseAsync(code, null, cancellationToken))
        {
            return Result<CountryAdminDto>.Fail(FailureKind.Conflict, $"El codi «{code}» ja existeix.");
        }

        var row = await repository.CreateCountryAsync(
            request.Code,
            request.Name,
            request.IsActive,
            request.SortOrder,
            cancellationToken);

        return Result<CountryAdminDto>.Success(ToDto(row));
    }

    public async Task<Result<CountryAdminDto>> UpdateCountryAsync(Guid id, UpdateCountryRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetCountryByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return Result<CountryAdminDto>.Fail(FailureKind.NotFound, "No s’ha trobat el país.");
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await repository.IsCountryCodeInUseAsync(code, id, cancellationToken))
        {
            return Result<CountryAdminDto>.Fail(FailureKind.Conflict, $"El codi «{code}» ja existeix.");
        }

        var row = await repository.UpdateCountryAsync(
            id,
            request.Code,
            request.Name,
            request.IsActive,
            request.SortOrder,
            cancellationToken);

        if (row is null)
        {
            return Result<CountryAdminDto>.Fail(FailureKind.NotFound, "No s’ha trobat el país.");
        }

        return Result<CountryAdminDto>.Success(ToDto(row));
    }

    public async Task<Result<bool>> DeleteCountryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await repository.GetCountryByIdAsync(id, cancellationToken) is null)
        {
            return Result<bool>.Fail(FailureKind.NotFound, "No s’ha trobat el país.");
        }

        if (await repository.CountryHasCitiesAsync(id, cancellationToken))
        {
            return Result<bool>.Fail(FailureKind.Conflict, "No es pot esborrar un país que encara té ciutats.");
        }

        var deleted = await repository.DeleteCountryAsync(id, cancellationToken);
        return deleted
            ? Result<bool>.Success(true)
            : Result<bool>.Fail(FailureKind.NotFound, "No s’ha trobat el país.");
    }

    public async Task<IReadOnlyList<CityAdminDto>> ListCitiesAsync(Guid? countryId, CancellationToken cancellationToken = default)
    {
        var rows = await repository.ListCitiesAsync(countryId, cancellationToken);
        return rows.Select(ToDto).ToArray();
    }

    public async Task<CityAdminDto?> GetCityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await repository.GetCityByIdAsync(id, cancellationToken);
        return row is null ? null : ToDto(row);
    }

    public async Task<Result<CityAdminDto>> CreateCityAsync(CreateCityRequest request, CancellationToken cancellationToken = default)
    {
        if (await repository.GetCountryByIdAsync(request.CountryId, cancellationToken) is null)
        {
            return Result<CityAdminDto>.Fail(FailureKind.NotFound, "No s’ha trobat el país.");
        }

        try
        {
            var row = await repository.CreateCityAsync(
                request.CountryId,
                request.Name,
                request.Latitude,
                request.Longitude,
                request.IsActive,
                request.SortOrder,
                cancellationToken);

            return Result<CityAdminDto>.Success(ToDto(row));
        }
        catch (InvalidOperationException ex)
        {
            return Result<CityAdminDto>.Fail(FailureKind.Conflict, "Ja existeix una ciutat amb aquest nom en aquest país.");
        }
    }

    public async Task<Result<CityAdminDto>> UpdateCityAsync(Guid id, UpdateCityRequest request, CancellationToken cancellationToken = default)
    {
        if (await repository.GetCityByIdAsync(id, cancellationToken) is null)
        {
            return Result<CityAdminDto>.Fail(FailureKind.NotFound, "No s’ha trobat la ciutat.");
        }

        try
        {
            var row = await repository.UpdateCityAsync(
                id,
                request.Name,
                request.Latitude,
                request.Longitude,
                request.IsActive,
                request.SortOrder,
                cancellationToken);

            if (row is null)
            {
                return Result<CityAdminDto>.Fail(FailureKind.NotFound, "No s’ha trobat la ciutat.");
            }

            return Result<CityAdminDto>.Success(ToDto(row));
        }
        catch (InvalidOperationException ex)
        {
            return Result<CityAdminDto>.Fail(FailureKind.Conflict, "Ja existeix una ciutat amb aquest nom en aquest país.");
        }
    }

    public async Task<Result<bool>> DeleteCityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await repository.DeleteCityAsync(id, cancellationToken);
        return deleted
            ? Result<bool>.Success(true)
            : Result<bool>.Fail(FailureKind.NotFound, "No s’ha trobat la ciutat.");
    }

    private static CountryAdminDto ToDto(CountryRow row) =>
        new(
            row.Id,
            row.Code,
            row.Name,
            row.IsActive,
            row.SortOrder,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);

    private static CityAdminDto ToDto(CityRow row) =>
        new(
            row.Id,
            row.CountryId,
            row.CountryName,
            row.CountryCode,
            row.Name,
            row.Latitude,
            row.Longitude,
            row.IsActive,
            row.SortOrder,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);
}
