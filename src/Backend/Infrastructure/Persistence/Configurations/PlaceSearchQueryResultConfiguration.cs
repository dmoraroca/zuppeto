using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Configurations;

public sealed class PlaceSearchQueryResultConfiguration : IEntityTypeConfiguration<PlaceSearchQueryResultRecord>
{
    public void Configure(EntityTypeBuilder<PlaceSearchQueryResultRecord> builder)
    {
        builder.ToTable("place_search_query_results");

        builder.HasKey(row => new { row.QueryId, row.PlaceId });

        builder.Property(row => row.QueryId)
            .HasColumnName("query_id")
            .IsRequired();

        builder.Property(row => row.PlaceId)
            .HasColumnName("place_id")
            .IsRequired();

        builder.Property(row => row.Rank)
            .HasColumnName("rank")
            .IsRequired();

        builder.Property(row => row.CapturedAtUtc)
            .HasColumnName("captured_at_utc")
            .IsRequired();

        builder.HasOne(row => row.Query)
            .WithMany(q => q.Results)
            .HasForeignKey(row => row.QueryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(row => row.Place)
            .WithMany()
            .HasForeignKey(row => row.PlaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(row => new { row.QueryId, row.Rank })
            .HasDatabaseName("ix_place_search_query_results_query_rank");
    }
}
