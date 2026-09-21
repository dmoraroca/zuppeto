using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class TerritorialDatasetSourceConfiguration : IEntityTypeConfiguration<TerritorialDatasetSourceRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialDatasetSourceRecord> builder)
    {
        builder.ToTable("territorial_dataset_sources");
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.Id, item.CountryId })
            .HasName("ak_territorial_dataset_sources_id_country_id");

        builder.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(item => item.CountryId).HasColumnName("country_id").IsRequired();
        builder.Property(item => item.Organisation).HasColumnName("organisation").HasMaxLength(240).IsRequired();
        builder.Property(item => item.Dataset).HasColumnName("dataset").HasMaxLength(240).IsRequired();
        builder.Property(item => item.Url).HasColumnName("url").HasMaxLength(2048).IsRequired();
        builder.Property(item => item.DownloadUrl).HasColumnName("download_url").HasMaxLength(2048);
        builder.Property(item => item.License).HasColumnName("license").HasMaxLength(500);
        builder.Property(item => item.LicenseUrl).HasColumnName("license_url").HasMaxLength(2048);
        builder.Property(item => item.Attribution).HasColumnName("attribution").HasMaxLength(1000);
        builder.Property(item => item.CommercialUseAllowed).HasColumnName("commercial_use_allowed");
        builder.Property(item => item.TransformationAllowed).HasColumnName("transformation_allowed");
        builder.Property(item => item.Restrictions).HasColumnName("restrictions");
        builder.Property(item => item.ThirdPartyData).HasColumnName("third_party_data");
        builder.Property(item => item.ApprovalStatus).HasColumnName("approval_status").HasMaxLength(20).IsRequired();
        builder.Property(item => item.VerifiedAtUtc).HasColumnName("verified_at_utc");
        builder.Property(item => item.VerifiedByUserId).HasColumnName("verified_by_user_id");
        builder.Property(item => item.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(item => item.PublicationMode).HasColumnName("publication_mode").HasMaxLength(20).IsRequired();
        builder.Property(item => item.DatasetVersion).HasColumnName("dataset_version").HasMaxLength(120);
        builder.Property(item => item.DatasetDate).HasColumnName("dataset_date").HasColumnType("date");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();

        builder.HasOne(item => item.Country)
            .WithMany(country => country.TerritorialDatasetSources)
            .HasForeignKey(item => item.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.VerifiedByUser)
            .WithMany()
            .HasForeignKey(item => item.VerifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.CountryId).HasDatabaseName("ix_territorial_dataset_sources_country_id");
        builder.HasIndex(item => new { item.CountryId, item.Organisation, item.Dataset, item.DatasetVersion })
            .HasDatabaseName("ix_territorial_dataset_sources_identity");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_territorial_dataset_sources_approval_status", "approval_status IN ('Pending', 'Approved', 'Rejected')");
            table.HasCheckConstraint("ck_territorial_dataset_sources_publication_mode", "publication_mode IN ('FullSnapshot', 'Delta')");
            table.HasCheckConstraint("ck_territorial_dataset_sources_verification", "approval_status = 'Pending' OR (verified_at_utc IS NOT NULL AND verified_by_user_id IS NOT NULL)");
        });
    }
}
