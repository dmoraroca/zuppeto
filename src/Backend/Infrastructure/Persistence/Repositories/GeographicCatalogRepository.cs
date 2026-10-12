using Microsoft.EntityFrameworkCore;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Geography;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

internal sealed class GeographicCatalogRepository(ZuppetoDbContext dbContext) : IGeographicCatalogRepository
{
    public async Task<IReadOnlyList<CountryRow>> ListCountriesAsync(CancellationToken cancellationToken = default)
    {
        var records = await dbContext.Countries
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return records.Select(ToRow).ToArray();
    }

    public async Task<CountryRow?> GetCountryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return record is null ? null : ToRow(record);
    }

    public Task<bool> IsCountryCodeInUseAsync(string normalizedCode, Guid? exceptCountryId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Countries.AsNoTracking().Where(c => c.Code == normalizedCode);
        if (exceptCountryId is not null)
        {
            query = query.Where(c => c.Id != exceptCountryId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<CountryRow> CreateCountryAsync(
        string code,
        string name,
        bool isActive,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCountryCode(code);
        var now = DateTimeOffset.UtcNow;
        var entity = new CountryRecord
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            Name = name.Trim(),
            IsActive = isActive,
            SortOrder = sortOrder,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await dbContext.Countries.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToRow(entity);
    }

    public async Task<CountryRow?> UpdateCountryAsync(
        Guid id,
        string code,
        string name,
        bool isActive,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var normalizedCode = NormalizeCountryCode(code);
        entity.Code = normalizedCode;
        entity.Name = name.Trim();
        entity.IsActive = isActive;
        entity.SortOrder = sortOrder;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToRow(entity);
    }

    public async Task<bool> CountryHasTerritorialDependenciesAsync(Guid countryId, CancellationToken cancellationToken = default) =>
        await dbContext.TerritorialUnits.AnyAsync(x => x.CountryId == countryId, cancellationToken)
        || await dbContext.TerritorialUnitTypes.AnyAsync(x => x.CountryId == countryId, cancellationToken)
        || await dbContext.TerritorialDatasetSources.AnyAsync(x => x.CountryId == countryId, cancellationToken)
        || await dbContext.Users.AnyAsync(x => x.TerritorialCountryId == countryId, cancellationToken)
        || await dbContext.Places.AnyAsync(x => x.TerritorialCountryId == countryId, cancellationToken);

    public async Task<bool> DeleteCountryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        if (await CountryHasTerritorialDependenciesAsync(id, cancellationToken))
        {
            return false;
        }

        dbContext.Countries.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static CountryRow ToRow(CountryRecord record) =>
        new(
            record.Id,
            record.Code,
            record.Name,
            record.IsActive,
            record.SortOrder,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    private static string NormalizeCountryCode(string code) =>
        CountryCodeRules.Normalize(code).ToUpperInvariant();
}
