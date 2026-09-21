using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class CountryConfiguration : IEntityTypeConfiguration<CountryRecord>
{
    public void Configure(EntityTypeBuilder<CountryRecord> builder)
    {
        builder.ToTable("countries");

        builder.HasKey(country => country.Id);

        builder.Property(country => country.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(country => country.Code)
            .HasColumnName("code")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(country => country.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(country => country.Iso2)
            .HasColumnName("iso2")
            .HasMaxLength(2);

        builder.Property(country => country.Iso3)
            .HasColumnName("iso3")
            .HasMaxLength(3);

        builder.Property(country => country.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(country => country.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(country => country.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(country => country.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(country => country.Code)
            .IsUnique()
            .HasDatabaseName("uq_countries_code");

        builder.HasIndex(country => country.Iso2)
            .IsUnique()
            .HasFilter("iso2 IS NOT NULL")
            .HasDatabaseName("uq_countries_iso2");

        builder.HasIndex(country => country.Iso3)
            .IsUnique()
            .HasFilter("iso3 IS NOT NULL")
            .HasDatabaseName("uq_countries_iso3");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_countries_iso2", "iso2 IS NULL OR iso2 ~ '^[A-Z]{2}$'");
            table.HasCheckConstraint("ck_countries_iso3", "iso3 IS NULL OR iso3 ~ '^[A-Z]{3}$'");
        });
    }
}
