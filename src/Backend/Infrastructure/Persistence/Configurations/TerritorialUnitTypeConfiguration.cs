using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class TerritorialUnitTypeConfiguration : IEntityTypeConfiguration<TerritorialUnitTypeRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialUnitTypeRecord> builder)
    {
        builder.ToTable("territorial_unit_types");
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.Id, item.CountryId })
            .HasName("ak_territorial_unit_types_id_country_id");

        builder.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(item => item.CountryId).HasColumnName("country_id").IsRequired();
        builder.Property(item => item.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(item => item.DisplayOrder).HasColumnName("display_order").IsRequired();
        builder.Property(item => item.IsSelectableLocality).HasColumnName("is_selectable_locality").IsRequired();
        builder.Property(item => item.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();

        builder.HasOne(item => item.Country)
            .WithMany(country => country.TerritorialUnitTypes)
            .HasForeignKey(item => item.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new { item.CountryId, item.Code })
            .IsUnique()
            .HasDatabaseName("uq_territorial_unit_types_country_code");
        builder.HasIndex(item => new { item.CountryId, item.DisplayOrder })
            .HasDatabaseName("ix_territorial_unit_types_country_order");
    }
}
