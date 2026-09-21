using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class TerritorialLocaleAssignmentConfiguration : IEntityTypeConfiguration<TerritorialLocaleAssignmentRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialLocaleAssignmentRecord> builder)
    {
        builder.ToTable("territorial_locale_assignments");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(item => item.CountryId).HasColumnName("country_id").IsRequired();
        builder.Property(item => item.TerritorialUnitId).HasColumnName("territorial_unit_id");
        builder.Property(item => item.Locale).HasColumnName("locale").HasMaxLength(35).IsRequired();
        builder.Property(item => item.IsOfficial).HasColumnName("is_official").IsRequired();
        builder.Property(item => item.Priority).HasColumnName("priority").IsRequired();
        builder.Property(item => item.DatasetSourceId).HasColumnName("dataset_source_id");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();

        builder.HasOne(item => item.Country)
            .WithMany(country => country.TerritorialLocaleAssignments)
            .HasForeignKey(item => item.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.TerritorialUnit)
            .WithMany(unit => unit.LocaleAssignments)
            .HasForeignKey(item => new { item.TerritorialUnitId, item.CountryId })
            .HasPrincipalKey(unit => new { unit.Id, unit.CountryId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.DatasetSource)
            .WithMany(source => source.LocaleAssignments)
            .HasForeignKey(item => item.DatasetSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new { item.CountryId, item.Locale })
            .IsUnique()
            .HasFilter("territorial_unit_id IS NULL")
            .HasDatabaseName("uq_territorial_locales_country");
        builder.HasIndex(item => new { item.TerritorialUnitId, item.Locale })
            .IsUnique()
            .HasFilter("territorial_unit_id IS NOT NULL")
            .HasDatabaseName("uq_territorial_locales_unit");
    }
}
