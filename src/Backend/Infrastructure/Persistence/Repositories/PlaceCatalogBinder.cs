using Microsoft.EntityFrameworkCore;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

/// <summary>
/// Binds place tags/features to catalog rows (insert missing, never UPDATE a phantom row).
/// </summary>
internal sealed class PlaceCatalogBinder(ZuppetoDbContext dbContext)
{
    internal async Task BindAsync(PlaceRecord record, CancellationToken cancellationToken)
    {
        await BindTagsAsync(record, cancellationToken);
        await BindFeaturesAsync(record, cancellationToken);
    }

    private async Task BindTagsAsync(PlaceRecord record, CancellationToken cancellationToken)
    {
        foreach (var placeTag in record.PlaceTags.ToArray())
        {
            var tagCode = placeTag.Tag.Code.Trim();
            var existingTag = dbContext.Tags.Local.FirstOrDefault(tag =>
                    tag.Id != Guid.Empty && tag.Code == tagCode)
                ?? await dbContext.Tags.FirstOrDefaultAsync(tag => tag.Code == tagCode, cancellationToken);

            DetachIfTracked(placeTag.Tag);
            if (existingTag is null)
            {
                var created = new TagRecord
                {
                    Id = Guid.NewGuid(),
                    Code = tagCode,
                    DisplayName = string.IsNullOrWhiteSpace(placeTag.Tag.DisplayName)
                        ? tagCode
                        : placeTag.Tag.DisplayName.Trim()
                };
                dbContext.Tags.Add(created);
                placeTag.Tag = created;
                placeTag.TagId = created.Id;
                continue;
            }

            placeTag.Tag = existingTag;
            placeTag.TagId = existingTag.Id;
        }
    }

    private async Task BindFeaturesAsync(PlaceRecord record, CancellationToken cancellationToken)
    {
        foreach (var placeFeature in record.PlaceFeatures.ToArray())
        {
            var featureCode = placeFeature.Feature.Code.Trim();
            var existingFeature = dbContext.Features.Local.FirstOrDefault(feature =>
                    feature.Id != Guid.Empty && feature.Code == featureCode)
                ?? await dbContext.Features.FirstOrDefaultAsync(
                    feature => feature.Code == featureCode,
                    cancellationToken);

            DetachIfTracked(placeFeature.Feature);
            if (existingFeature is null)
            {
                var created = new FeatureRecord
                {
                    Id = Guid.NewGuid(),
                    Code = featureCode,
                    DisplayName = string.IsNullOrWhiteSpace(placeFeature.Feature.DisplayName)
                        ? featureCode
                        : placeFeature.Feature.DisplayName.Trim()
                };
                dbContext.Features.Add(created);
                placeFeature.Feature = created;
                placeFeature.FeatureId = created.Id;
                continue;
            }

            placeFeature.Feature = existingFeature;
            placeFeature.FeatureId = existingFeature.Id;
        }
    }

    private void DetachIfTracked(object entity)
    {
        var entry = dbContext.ChangeTracker.Entries()
            .FirstOrDefault(current => ReferenceEquals(current.Entity, entity));
        if (entry is not null && entry.State != EntityState.Detached)
        {
            entry.State = EntityState.Detached;
        }
    }
}
