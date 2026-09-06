using Microsoft.EntityFrameworkCore;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Geography;
using Zuppeto.Domain.Places;
using Zuppeto.Infrastructure.Persistence.Entities;
using Zuppeto.Infrastructure.Persistence.Specifications;
using Zuppeto.Infrastructure.Persistence.Mappings;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

internal sealed class PlaceRepository(ZuppetoDbContext dbContext) : IPlaceRepository
{
    private readonly PlaceCatalogBinder catalogBinder = new(dbContext);

    public async Task<Place?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await BuildGraphQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(place => place.Id == id, cancellationToken);

        return record is null ? null : PlacePersistenceMapper.ToDomain(record);
    }

    public async Task<Place?> GetByGooglePlaceIdAsync(string googlePlaceId, CancellationToken cancellationToken = default)
    {
        var normalized = googlePlaceId.Trim();
        if (normalized.Length == 0)
        {
            return null;
        }

        var record = await BuildGraphQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(place => place.GooglePlaceId == normalized, cancellationToken);

        return record is null ? null : PlacePersistenceMapper.ToDomain(record);
    }

    public async Task<IReadOnlyCollection<Place>> SearchAsync(
        PlaceSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PlaceRecord> query = BuildGraphQuery().AsNoTracking();
        var specification = new PlaceSearchSpecification(criteria);
        query = specification.Apply(query);

        var records = await query
            .OrderBy(place => place.City)
            .ThenBy(place => place.Name)
            .ToArrayAsync(cancellationToken);

        return records
            .Select(PlacePersistenceMapper.ToDomain)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<Place>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var records = await BuildGraphQuery()
            .AsNoTracking()
            .Where(place => ids.Contains(place.Id))
            .ToArrayAsync(cancellationToken);

        var placesById = records.ToDictionary(record => record.Id, PlacePersistenceMapper.ToDomain);

        return ids
            .Where(placesById.ContainsKey)
            .Select(id => placesById[id])
            .ToArray();
    }

    public async Task<IReadOnlyCollection<string>> GetAvailableCitiesAsync(CancellationToken cancellationToken = default)
    {
        var codes = EuropeanCountryCodes.Iso3166Alpha2;

        return await dbContext.Places
            .AsNoTracking()
            .Where(place => place.City != string.Empty)
            .Where(place => dbContext.Countries
                .Any(c => codes.Contains(c.Code) && c.Name.ToLower() == place.Country.ToLower()))
            .Select(place => place.City)
            .Distinct()
            .OrderBy(city => city)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<IPlaceRepository.CityCatalogItem>> SearchAvailableCitiesAsync(
        string normalizedQueryFragment,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(normalizedQueryFragment))
        {
            return [];
        }

        var pattern = $"%{normalizedQueryFragment}%";
        var codes = EuropeanCountryCodes.Iso3166Alpha2;

        var rows = await dbContext.Places
            .AsNoTracking()
            .Where(place => place.City != string.Empty)
            .Where(place => EF.Functions.ILike(place.City, pattern))
            .Where(place => dbContext.Countries
                .Any(c => codes.Contains(c.Code) && c.Name.ToLower() == place.Country.ToLower()))
            .Select(place => new { place.City, place.Country })
            .Distinct()
            .OrderBy(row => row.City)
            .ThenBy(row => row.Country)
            .Take(limit)
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(row => new IPlaceRepository.CityCatalogItem(row.City, row.Country))
            .ToArray();
    }

    public async Task AddAsync(Place place, CancellationToken cancellationToken = default)
    {
        var record = PlacePersistenceMapper.ToRecord(place);
        PlacePersistenceMapper.SyncCollections(place, record);
        var nowUtc = DateTimeOffset.UtcNow;
        record.CreatedAtUtc = nowUtc;
        record.UpdatedAtUtc = nowUtc;

        await catalogBinder.BindAsync(record, cancellationToken);

        await dbContext.Places.AddAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Place place, CancellationToken cancellationToken = default)
    {
        var record = await BuildGraphQuery()
            .FirstOrDefaultAsync(current => current.Id == place.Id, cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException("No s’ha trobat el lloc.");
        }

        PlacePersistenceMapper.Apply(place, record);
        PlacePersistenceMapper.SyncCollections(place, record);
        record.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await catalogBinder.BindAsync(record, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Places
            .FirstOrDefaultAsync(place => place.Id == id, cancellationToken);

        if (record is null)
        {
            return false;
        }

        dbContext.Places.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<PlaceRecord> BuildGraphQuery()
    {
        return dbContext.Places
            .Include(place => place.PlaceTags)
                .ThenInclude(placeTag => placeTag.Tag)
            .Include(place => place.PlaceFeatures)
                .ThenInclude(placeFeature => placeFeature.Feature);
    }
}
