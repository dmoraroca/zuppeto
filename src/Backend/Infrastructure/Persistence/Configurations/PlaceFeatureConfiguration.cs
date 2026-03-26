using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Configurations;

public sealed class PlaceFeatureConfiguration : IEntityTypeConfiguration<PlaceFeatureRecord>
{
    public void Configure(EntityTypeBuilder<PlaceFeatureRecord> builder)
    {
        builder.ToTable("place_features");

        builder.HasKey(record => new { record.PlaceId, record.FeatureId });

        builder.Property(record => record.PlaceId)
            .HasColumnName("place_id");

        builder.Property(record => record.FeatureId)
            .HasColumnName("feature_id");

        builder.HasIndex(record => record.FeatureId)
            .HasDatabaseName("ix_place_features_feature_id");

        builder.HasOne(record => record.Place)
            .WithMany(place => place.PlaceFeatures)
            .HasForeignKey(record => record.PlaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(record => record.Feature)
            .WithMany(feature => feature.PlaceFeatures)
            .HasForeignKey(record => record.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
