using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Configurations;

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
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(country => country.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

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
    }
}
