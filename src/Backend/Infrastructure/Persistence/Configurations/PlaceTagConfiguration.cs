using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Configurations;

public sealed class PlaceTagConfiguration : IEntityTypeConfiguration<PlaceTagRecord>
{
    public void Configure(EntityTypeBuilder<PlaceTagRecord> builder)
    {
        builder.ToTable("place_tags");

        builder.HasKey(record => new { record.PlaceId, record.TagId });

        builder.Property(record => record.PlaceId)
            .HasColumnName("place_id");

        builder.Property(record => record.TagId)
            .HasColumnName("tag_id");

        builder.HasIndex(record => record.TagId)
            .HasDatabaseName("ix_place_tags_tag_id");

        builder.HasOne(record => record.Place)
            .WithMany(place => place.PlaceTags)
            .HasForeignKey(record => record.PlaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(record => record.Tag)
            .WithMany(tag => tag.PlaceTags)
            .HasForeignKey(record => record.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
