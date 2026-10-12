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
        var code = CountryCodeRules.Normalize(request.Code).ToUpperInvariant();
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

        var code = CountryCodeRules.Normalize(request.Code).ToUpperInvariant();
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

        if (await repository.CountryHasTerritorialDependenciesAsync(id, cancellationToken))
        {
            return Result<bool>.Fail(FailureKind.Conflict, "No es pot esborrar un país que té dades territorials o referències actives.");
        }

        var deleted = await repository.DeleteCountryAsync(id, cancellationToken);
        return deleted
            ? Result<bool>.Success(true)
            : Result<bool>.Fail(FailureKind.NotFound, "No s’ha trobat el país.");
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
}
