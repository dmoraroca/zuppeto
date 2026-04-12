using Microsoft.EntityFrameworkCore;
using YepPet.Domain.Abstractions;
using YepPet.Domain.Geography;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Repositories;

internal sealed class GeographicCatalogRepository(YepPetDbContext dbContext) : IGeographicCatalogRepository
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

    public Task<bool> IsCityNormalizedNameInUseAsync(
        Guid countryId,
        string normalizedName,
        Guid? exceptCityId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Cities.AsNoTracking()
            .Where(c => c.CountryId == countryId && c.NormalizedName == normalizedName);
        if (exceptCityId is not null)
        {
            query = query.Where(c => c.Id != exceptCityId.Value);
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

    public Task<bool> CountryHasCitiesAsync(Guid countryId, CancellationToken cancellationToken = default) =>
        dbContext.Cities.AnyAsync(c => c.CountryId == countryId, cancellationToken);

    public async Task<bool> DeleteCountryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        if (await dbContext.Cities.AnyAsync(c => c.CountryId == id, cancellationToken))
        {
            return false;
        }

        dbContext.Countries.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<CityRow>> ListCitiesAsync(Guid? countryId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Cities
            .AsNoTracking()
            .Include(c => c.Country)
            .AsQueryable();

        if (countryId is not null)
        {
            query = query.Where(c => c.CountryId == countryId.Value);
        }

        var records = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return records.Select(ToRow).ToArray();
    }

    public async Task<CityRow?> GetCityByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Cities
            .AsNoTracking()
            .Include(c => c.Country)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return record is null ? null : ToRow(record);
    }

    public async Task<CityRow> CreateCityAsync(
        Guid countryId,
        string name,
        decimal? latitude,
        decimal? longitude,
        bool isActive,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        var countryExists = await dbContext.Countries.AnyAsync(c => c.Id == countryId, cancellationToken);
        if (!countryExists)
        {
            throw new InvalidOperationException("Country not found.");
        }

        var trimmed = name.Trim();
        var normalized = NormalizeCityName(trimmed);
        if (await IsCityNormalizedNameInUseAsync(countryId, normalized, null, cancellationToken))
        {
            throw new InvalidOperationException("City name already exists in this country.");
        }
        var now = DateTimeOffset.UtcNow;
        var entity = new CityRecord
        {
            Id = Guid.NewGuid(),
            CountryId = countryId,
            Name = trimmed,
            NormalizedName = normalized,
            Latitude = latitude,
            Longitude = longitude,
            IsActive = isActive,
            SortOrder = sortOrder,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await dbContext.Cities.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(entity).Reference(e => e.Country).LoadAsync(cancellationToken);
        return ToRow(entity);
    }

    public async Task<CityRow?> UpdateCityAsync(
        Guid id,
        string name,
        decimal? latitude,
        decimal? longitude,
        bool isActive,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Cities
            .Include(c => c.Country)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var trimmed = name.Trim();
        var normalized = NormalizeCityName(trimmed);
        if (await IsCityNormalizedNameInUseAsync(entity.CountryId, normalized, id, cancellationToken))
        {
            throw new InvalidOperationException("City name already exists in this country.");
        }

        entity.Name = trimmed;
        entity.NormalizedName = normalized;
        entity.Latitude = latitude;
        entity.Longitude = longitude;
        entity.IsActive = isActive;
        entity.SortOrder = sortOrder;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToRow(entity);
    }

    public async Task<bool> DeleteCityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Cities.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        dbContext.Cities.Remove(entity);
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

    private static CityRow ToRow(CityRecord record) =>
        new(
            record.Id,
            record.CountryId,
            record.Country.Name,
            record.Country.Code,
            record.Name,
            record.NormalizedName,
            record.Latitude,
            record.Longitude,
            record.IsActive,
            record.SortOrder,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    private static string NormalizeCountryCode(string code) => code.Trim().ToUpperInvariant();

    private static string NormalizeCityName(string name) => name.Trim().ToUpperInvariant();
}
