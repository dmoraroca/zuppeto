using Microsoft.EntityFrameworkCore;
using YepPet.Domain.Abstractions;
using YepPet.Domain.Places;
using YepPet.Infrastructure.Persistence.Entities;
using YepPet.Infrastructure.Persistence.Specifications;
using YepPet.Infrastructure.Persistence.Mappings;

namespace YepPet.Infrastructure.Persistence.Repositories;

internal sealed class PlaceRepository(YepPetDbContext dbContext) : IPlaceRepository
{
    public async Task<Place?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await BuildGraphQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(place => place.Id == id, cancellationToken);

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
        return await dbContext.Places
            .AsNoTracking()
            .Select(place => place.City)
            .Where(city => city != string.Empty)
            .Distinct()
            .OrderBy(city => city)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(Place place, CancellationToken cancellationToken = default)
    {
        var record = PlacePersistenceMapper.ToRecord(place);
        PlacePersistenceMapper.SyncCollections(place, record);

        await AttachTagCatalogAsync(record, cancellationToken);
        await AttachFeatureCatalogAsync(record, cancellationToken);

        await dbContext.Places.AddAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Place place, CancellationToken cancellationToken = default)
    {
        var record = await BuildGraphQuery()
            .FirstOrDefaultAsync(current => current.Id == place.Id, cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException($"Place '{place.Id}' was not found.");
        }

        PlacePersistenceMapper.Apply(place, record);
        PlacePersistenceMapper.SyncCollections(place, record);

        await AttachTagCatalogAsync(record, cancellationToken);
        await AttachFeatureCatalogAsync(record, cancellationToken);

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

    private async Task AttachTagCatalogAsync(PlaceRecord record, CancellationToken cancellationToken)
    {
        foreach (var placeTag in record.PlaceTags)
        {
            var tagCode = placeTag.Tag.Code;
            var existingTag = await dbContext.Tags
                .FirstOrDefaultAsync(tag => tag.Code == tagCode, cancellationToken);

            if (existingTag is null)
            {
                placeTag.Tag.Id = Guid.NewGuid();
                continue;
            }

            placeTag.Tag = existingTag;
            placeTag.TagId = existingTag.Id;
        }
    }

    private async Task AttachFeatureCatalogAsync(PlaceRecord record, CancellationToken cancellationToken)
    {
        foreach (var placeFeature in record.PlaceFeatures)
        {
            var featureCode = placeFeature.Feature.Code;
            var existingFeature = await dbContext.Features
                .FirstOrDefaultAsync(feature => feature.Code == featureCode, cancellationToken);

            if (existingFeature is null)
            {
                placeFeature.Feature.Id = Guid.NewGuid();
                continue;
            }

            placeFeature.Feature = existingFeature;
            placeFeature.FeatureId = existingFeature.Id;
        }
    }
}
