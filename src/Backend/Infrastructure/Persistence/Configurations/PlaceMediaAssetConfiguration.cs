using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class PlaceMediaAssetConfiguration : IEntityTypeConfiguration<PlaceMediaAssetRecord>
{
    public void Configure(EntityTypeBuilder<PlaceMediaAssetRecord> builder)
    {
        builder.ToTable("place_media_assets", table =>
        {
            table.HasCheckConstraint("ck_place_media_source", "source IN ('PetiLoc','Owner','User','WikimediaCommons','Mapillary','OfficialWebsite')");
            table.HasCheckConstraint("ck_place_media_verification", "verification_status IN ('Pending','Verified','Rejected')");
        });
        builder.HasKey(asset => asset.Id);
        builder.Property(asset => asset.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(asset => asset.PlaceId).HasColumnName("place_id");
        builder.HasOne(asset => asset.Place).WithMany().HasForeignKey(asset => asset.PlaceId).OnDelete(DeleteBehavior.Cascade);
        Text(builder, asset => asset.Source, "source", 40);
        Text(builder, asset => asset.SourceOrganisation, "source_organisation", 200);
        Text(builder, asset => asset.ExternalId, "external_id", 500);
        Text(builder, asset => asset.OriginalUrl, "original_url", 2048);
        Text(builder, asset => asset.ServedUrl, "served_url", 2048);
        Text(builder, asset => asset.Author, "author", 500);
        Text(builder, asset => asset.LicenseIdentifier, "license_identifier", 100);
        Optional(builder, asset => asset.LicenseVersion, "license_version", 20);
        Optional(builder, asset => asset.LicenseUrl, "license_url", 2048);
        builder.Property(asset => asset.Attribution).HasColumnName("attribution").HasMaxLength(2000).IsRequired();
        builder.Property(asset => asset.RetrievedAtUtc).HasColumnName("retrieved_at_utc");
        builder.Property(asset => asset.PermanentStorageAllowed).HasColumnName("permanent_storage_allowed");
        builder.Property(asset => asset.TransformationAllowed).HasColumnName("transformation_allowed");
        Text(builder, asset => asset.ContentHash, "content_hash", 128);
        Text(builder, asset => asset.VerificationStatus, "verification_status", 24);
        builder.Property(asset => asset.CanBePrimary).HasColumnName("can_be_primary");
        builder.Property(asset => asset.IsPrimary).HasColumnName("is_primary");
        builder.Property(asset => asset.IdentityVerified).HasColumnName("identity_verified");
        builder.Property(asset => asset.ReuseConditions).HasColumnName("reuse_conditions").HasMaxLength(2000).IsRequired();
        Optional(builder, asset => asset.WikidataQid, "wikidata_qid", 20);
        Optional(builder, asset => asset.WikimediaFileName, "wikimedia_file_name", 500);
        Optional(builder, asset => asset.EvidenceUrl, "evidence_url", 2048);
        Optional(builder, asset => asset.RejectionReason, "rejection_reason", 1000);
        builder.Property(asset => asset.CapturedAtUtc).HasColumnName("captured_at_utc");
        builder.Property(asset => asset.Latitude).HasColumnName("latitude").HasPrecision(9, 6);
        builder.Property(asset => asset.Longitude).HasColumnName("longitude").HasPrecision(9, 6);
        builder.Property(asset => asset.HeadingDegrees).HasColumnName("heading_degrees").HasPrecision(6, 2);
        builder.Property(asset => asset.DistanceMeters).HasColumnName("distance_meters");
        builder.Property(asset => asset.LastCheckedAtUtc).HasColumnName("last_checked_at_utc");
        builder.Property(asset => asset.ModeratedAtUtc).HasColumnName("moderated_at_utc");
        builder.Property(asset => asset.ModeratorActorId).HasColumnName("moderator_actor_id");
        builder.Property(asset => asset.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("NOW()");
        builder.Property(asset => asset.UpdatedAtUtc).HasColumnName("updated_at_utc").HasDefaultValueSql("NOW()");
        builder.HasIndex(asset => new { asset.PlaceId, asset.ContentHash }).IsUnique().HasDatabaseName("ux_place_media_place_hash");
        builder.HasIndex(asset => asset.PlaceId).HasDatabaseName("ix_place_media_place");
        builder.HasIndex(asset => new { asset.PlaceId, asset.IsPrimary })
            .IsUnique().HasFilter("is_primary = TRUE").HasDatabaseName("ux_place_media_primary");
    }

    private static void Text<T>(EntityTypeBuilder<T> builder, System.Linq.Expressions.Expression<Func<T, string>> property, string name, int length) where T : class =>
        builder.Property(property).HasColumnName(name).HasMaxLength(length).IsRequired();

    private static void Optional<T>(EntityTypeBuilder<T> builder, System.Linq.Expressions.Expression<Func<T, string?>> property, string name, int length) where T : class =>
        builder.Property(property).HasColumnName(name).HasMaxLength(length);
}

public sealed class PlaceMediaDiscoveryConfiguration : IEntityTypeConfiguration<PlaceMediaDiscoveryRecord>
{
    public void Configure(EntityTypeBuilder<PlaceMediaDiscoveryRecord> builder)
    {
        builder.ToTable("place_media_discoveries", table =>
            table.HasCheckConstraint("ck_place_media_discovery_status", "status IN ('Completed','Failed')"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(item => item.PlaceId).HasColumnName("place_id");
        builder.HasOne(item => item.Place).WithMany().HasForeignKey(item => item.PlaceId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(item => item.Source).HasColumnName("source").HasMaxLength(40).IsRequired();
        builder.Property(item => item.LastCheckedAtUtc).HasColumnName("last_checked_at_utc");
        builder.Property(item => item.NextCheckAtUtc).HasColumnName("next_check_at_utc");
        builder.Property(item => item.CandidateCount).HasColumnName("candidate_count");
        builder.Property(item => item.Status).HasColumnName("status").HasMaxLength(24).IsRequired();
        builder.Property(item => item.Error).HasColumnName("error").HasMaxLength(1000);
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("NOW()");
        builder.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc").HasDefaultValueSql("NOW()");
        builder.HasIndex(item => new { item.PlaceId, item.Source }).IsUnique().HasDatabaseName("ux_place_media_discovery_place_source");
        builder.HasIndex(item => item.NextCheckAtUtc).HasDatabaseName("ix_place_media_discovery_next_check");
    }
}
