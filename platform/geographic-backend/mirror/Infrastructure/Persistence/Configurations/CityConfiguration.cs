using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Configurations;

public sealed class CityConfiguration : IEntityTypeConfiguration<CityRecord>
{
    public void Configure(EntityTypeBuilder<CityRecord> builder)
    {
        builder.ToTable("cities");

        builder.HasKey(city => city.Id);

        builder.Property(city => city.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(city => city.CountryId)
            .HasColumnName("country_id")
            .IsRequired();

        builder.Property(city => city.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(city => city.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(city => city.Latitude)
            .HasColumnName("latitude")
            .HasPrecision(9, 6);

        builder.Property(city => city.Longitude)
            .HasColumnName("longitude")
            .HasPrecision(9, 6);

        builder.Property(city => city.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(city => city.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(city => city.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(city => city.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasOne(city => city.Country)
            .WithMany(country => country.Cities)
            .HasForeignKey(city => city.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(city => new { city.CountryId, city.NormalizedName })
            .IsUnique()
            .HasDatabaseName("uq_cities_country_normalized_name");
    }
}
