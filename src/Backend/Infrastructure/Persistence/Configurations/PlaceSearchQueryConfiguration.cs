using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Configurations;

public sealed class PlaceSearchQueryConfiguration : IEntityTypeConfiguration<PlaceSearchQueryRecord>
{
    public void Configure(EntityTypeBuilder<PlaceSearchQueryRecord> builder)
    {
        builder.ToTable("place_search_queries");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(q => q.QueryKey)
            .HasColumnName("query_key")
            .HasMaxLength(400)
            .IsRequired();

        builder.Property(q => q.SearchText)
            .HasColumnName("search_text")
            .HasMaxLength(240)
            .IsRequired();

        builder.Property(q => q.City)
            .HasColumnName("city")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(q => q.Type)
            .HasColumnName("type")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(q => q.PetCategory)
            .HasColumnName("pet_category")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(q => q.HitCount)
            .HasColumnName("hit_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(q => q.ResultCount)
            .HasColumnName("result_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(q => q.LastRunAtUtc)
            .HasColumnName("last_run_at_utc")
            .IsRequired();

        builder.Property(q => q.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();

        builder.Property(q => q.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(q => q.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(q => q.QueryKey)
            .IsUnique()
            .HasDatabaseName("uq_place_search_queries_query_key");
    }
}
