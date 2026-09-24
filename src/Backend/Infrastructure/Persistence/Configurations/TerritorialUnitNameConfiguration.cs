using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class TerritorialUnitNameConfiguration : IEntityTypeConfiguration<TerritorialUnitNameRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialUnitNameRecord> builder)
    {
        builder.ToTable("territorial_unit_names");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(item => item.TerritorialUnitId).HasColumnName("territorial_unit_id").IsRequired();
        builder.Property(item => item.Locale).HasColumnName("locale").HasMaxLength(35);
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
        builder.Property(item => item.Kind).HasColumnName("kind").HasMaxLength(20).IsRequired();
        builder.Property(item => item.IsPrimary).HasColumnName("is_primary").IsRequired();
        builder.Property(item => item.NormalizedName).HasColumnName("normalized_name").HasMaxLength(300).IsRequired();
        builder.Property(item => item.DatasetSourceId).HasColumnName("dataset_source_id");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();

        builder.HasOne(item => item.TerritorialUnit)
            .WithMany(unit => unit.Names)
            .HasForeignKey(item => item.TerritorialUnitId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.DatasetSource)
            .WithMany(source => source.Names)
            .HasForeignKey(item => item.DatasetSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.TerritorialUnitId).HasDatabaseName("ix_territorial_unit_names_unit_id");
        builder.HasIndex(item => item.NormalizedName).HasDatabaseName("ix_territorial_unit_names_normalized_name");
        builder.HasIndex(item => new { item.TerritorialUnitId, item.Locale, item.Kind })
            .IsUnique()
            .HasFilter("is_primary AND locale IS NOT NULL")
            .HasDatabaseName("uq_territorial_unit_names_primary_locale");
        builder.HasIndex(item => new { item.TerritorialUnitId, item.Kind })
            .IsUnique()
            .HasFilter("is_primary AND locale IS NULL")
            .HasDatabaseName("uq_territorial_unit_names_primary_no_locale");

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_territorial_unit_names_kind",
            "kind IN ('Official', 'Localized', 'Alternative', 'Historic')"));
    }
}
