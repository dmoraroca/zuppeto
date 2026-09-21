using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class TerritorialUnitCodeConfiguration : IEntityTypeConfiguration<TerritorialUnitCodeRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialUnitCodeRecord> builder)
    {
        builder.ToTable("territorial_unit_codes");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(item => item.TerritorialUnitId).HasColumnName("territorial_unit_id").IsRequired();
        builder.Property(item => item.Scheme).HasColumnName("scheme").HasMaxLength(160).IsRequired();
        builder.Property(item => item.Value).HasColumnName("value").HasMaxLength(160).IsRequired();
        builder.Property(item => item.ValidFrom).HasColumnName("valid_from").HasColumnType("date");
        builder.Property(item => item.ValidTo).HasColumnName("valid_to").HasColumnType("date");
        builder.Property(item => item.IsPrimary).HasColumnName("is_primary").IsRequired();
        builder.Property(item => item.DatasetSourceId).HasColumnName("dataset_source_id");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();

        builder.HasOne(item => item.TerritorialUnit)
            .WithMany(unit => unit.Codes)
            .HasForeignKey(item => item.TerritorialUnitId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.DatasetSource)
            .WithMany(source => source.Codes)
            .HasForeignKey(item => item.DatasetSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new { item.Scheme, item.Value })
            .IsUnique()
            .HasFilter("valid_to IS NULL")
            .HasDatabaseName("uq_territorial_unit_codes_current_scheme_value");
        builder.HasIndex(item => new { item.TerritorialUnitId, item.Scheme })
            .HasDatabaseName("ix_territorial_unit_codes_unit_scheme");

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_territorial_unit_codes_validity",
            "valid_from IS NULL OR valid_to IS NULL OR valid_to >= valid_from"));
    }
}
