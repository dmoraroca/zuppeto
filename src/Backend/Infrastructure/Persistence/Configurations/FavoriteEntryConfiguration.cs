using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Configurations;

public sealed class FavoriteEntryConfiguration : IEntityTypeConfiguration<FavoriteEntryRecord>
{
    public void Configure(EntityTypeBuilder<FavoriteEntryRecord> builder)
    {
        builder.ToTable("favorite_entries");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(record => record.FavoriteListId)
            .HasColumnName("favorite_list_id")
            .IsRequired();

        builder.Property(record => record.PlaceId)
            .HasColumnName("place_id")
            .IsRequired();

        builder.Property(record => record.SavedAtUtc)
            .HasColumnName("saved_at_utc")
            .IsRequired();

        builder.HasIndex(record => record.FavoriteListId)
            .HasDatabaseName("ix_favorite_entries_favorite_list_id");

        builder.HasIndex(record => new { record.FavoriteListId, record.PlaceId })
            .IsUnique()
            .HasDatabaseName("uq_favorite_entries_list_place");

        builder.HasOne(record => record.FavoriteList)
            .WithMany(favoriteList => favoriteList.Entries)
            .HasForeignKey(record => record.FavoriteListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(record => record.Place)
            .WithMany(place => place.FavoriteEntries)
            .HasForeignKey(record => record.PlaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
