using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class TerritorialUnitConfiguration : IEntityTypeConfiguration<TerritorialUnitRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialUnitRecord> builder)
    {
        builder.ToTable("territorial_units");
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.Id, item.CountryId })
            .HasName("ak_territorial_units_id_country_id");

        builder.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(item => item.CountryId).HasColumnName("country_id").IsRequired();
        builder.Property(item => item.ParentId).HasColumnName("parent_id");
        builder.Property(item => item.TerritorialUnitTypeId).HasColumnName("territorial_unit_type_id").IsRequired();
        builder.Property(item => item.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(item => item.Latitude).HasColumnName("latitude").HasPrecision(9, 6);
        builder.Property(item => item.Longitude).HasColumnName("longitude").HasPrecision(9, 6);
        builder.Property(item => item.CoordinateSourceId).HasColumnName("coordinate_source_id");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();

        builder.HasOne(item => item.Country)
            .WithMany(country => country.TerritorialUnits)
            .HasForeignKey(item => item.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Parent)
            .WithMany(parent => parent.Children)
            .HasForeignKey(item => new { item.ParentId, item.CountryId })
            .HasPrincipalKey(parent => new { parent.Id, parent.CountryId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.TerritorialUnitType)
            .WithMany(type => type.TerritorialUnits)
            .HasForeignKey(item => new { item.TerritorialUnitTypeId, item.CountryId })
            .HasPrincipalKey(type => new { type.Id, type.CountryId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.CoordinateSource)
            .WithMany(source => source.CoordinateUnits)
            .HasForeignKey(item => new { item.CoordinateSourceId, item.CountryId })
            .HasPrincipalKey(source => new { source.Id, source.CountryId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.CountryId).HasDatabaseName("ix_territorial_units_country_id");
        builder.HasIndex(item => item.ParentId).HasDatabaseName("ix_territorial_units_parent_id");
        builder.HasIndex(item => item.TerritorialUnitTypeId).HasDatabaseName("ix_territorial_units_type_id");
        builder.HasIndex(item => new { item.CountryId, item.TerritorialUnitTypeId, item.IsActive })
            .HasDatabaseName("ix_territorial_units_country_type_active");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_territorial_units_not_self_parent", "parent_id IS NULL OR parent_id <> id");
            table.HasCheckConstraint(
                "ck_territorial_units_coordinates",
                "(latitude IS NULL AND longitude IS NULL AND coordinate_source_id IS NULL) OR " +
                "(latitude IS NOT NULL AND longitude IS NOT NULL AND coordinate_source_id IS NOT NULL AND " +
                "latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180)");
        });
    }
}
