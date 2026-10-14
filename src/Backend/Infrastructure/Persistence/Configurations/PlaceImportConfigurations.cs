using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class PlaceDatasetSourceConfiguration : IEntityTypeConfiguration<PlaceDatasetSourceRecord>
{
    public void Configure(EntityTypeBuilder<PlaceDatasetSourceRecord> b)
    {
        b.ToTable("place_dataset_sources", t => t.HasCheckConstraint("ck_place_dataset_source_approval", "approval_status IN ('PilotApproved','Suspended')"));
        b.HasKey(x => x.Id); Id(b, x => x.Id);
        Text(b, x => x.SourceKey, "source_key", 120); b.HasIndex(x => x.SourceKey).IsUnique();
        Text(b, x => x.Organisation, "organisation", 240); Text(b, x => x.Dataset, "dataset", 240);
        Text(b, x => x.Url, "url", 2048); Text(b, x => x.AttributionUrl, "attribution_url", 2048);
        Text(b, x => x.LicenseIdentifier, "license_identifier", 80); Text(b, x => x.LicenseUrl, "license_url", 2048);
        Text(b, x => x.Attribution, "attribution", 500); b.Property(x => x.ReuseConditions).HasColumnName("reuse_conditions").IsRequired();
        b.Property(x => x.PermanentStorageAllowed).HasColumnName("permanent_storage_allowed"); b.Property(x => x.OdblDerived).HasColumnName("odbl_derived");
        Text(b, x => x.ApprovalStatus, "approval_status", 20); b.Property(x => x.ReviewedAtUtc).HasColumnName("reviewed_at_utc"); b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
    }
    private static void Id<T>(EntityTypeBuilder<T> b, System.Linq.Expressions.Expression<Func<T,Guid>> p) where T:class => b.Property(p).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
    private static void Text<T>(EntityTypeBuilder<T> b, System.Linq.Expressions.Expression<Func<T,string>> p, string n, int l) where T:class => b.Property(p).HasColumnName(n).HasMaxLength(l).IsRequired();
}

public sealed class PlaceImportRunConfiguration : IEntityTypeConfiguration<PlaceImportRunRecord>
{
    public void Configure(EntityTypeBuilder<PlaceImportRunRecord> b)
    {
        b.ToTable("place_import_runs", t =>
        {
            t.HasCheckConstraint("ck_place_import_status", "status IN ('Prepared','Running','Validated','PendingReview','Approved','Published','Failed','Cancelled')");
            t.HasCheckConstraint("ck_place_import_counts", "discovered_count >= 0 AND valid_count >= 0 AND warning_count >= 0 AND duplicate_count >= 0");
        });
        b.HasKey(x => x.Id); Id(b, x => x.Id);
        b.Property(x => x.SourceId).HasColumnName("source_id"); b.HasOne(x => x.Source).WithMany().HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.CountryId).HasColumnName("country_id"); b.HasOne(x => x.Country).WithMany().HasForeignKey(x => x.CountryId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.TerritorialUnitId).HasColumnName("territorial_unit_id"); b.HasOne(x => x.TerritorialUnit).WithMany().HasForeignKey(x => x.TerritorialUnitId).OnDelete(DeleteBehavior.Restrict);
        Text(b, x => x.CountryIso2, "country_iso2", 2); Text(b, x => x.TerritorialName, "territorial_name", 200);
        Text(b, x => x.OfficialCodeScheme, "official_code_scheme", 80); Text(b, x => x.OfficialCode, "official_code", 80);
        b.Property(x => x.CategoriesJson).HasColumnName("categories").HasColumnType("jsonb"); Text(b, x => x.AccessMode, "access_mode", 40); Text(b, x => x.Status, "status", 24); Text(b, x => x.Stage, "stage", 40);
        b.Property(x => x.SimulationOnly).HasColumnName("simulation_only"); Optional(b, x => x.BoundaryExternalId, "boundary_external_id", 120);
        b.Property(x => x.DiscoveredCount).HasColumnName("discovered_count"); b.Property(x => x.ValidCount).HasColumnName("valid_count"); b.Property(x => x.WarningCount).HasColumnName("warning_count"); b.Property(x => x.DuplicateCount).HasColumnName("duplicate_count");
        b.Property(x => x.CreatedByActorId).HasColumnName("created_by_actor_id"); b.HasOne(x => x.CreatedByActor).WithMany().HasForeignKey(x => x.CreatedByActorId).OnDelete(DeleteBehavior.Restrict);
        TimeFields(b); b.Property(x => x.CancellationRequested).HasColumnName("cancellation_requested"); Optional(b, x => x.LeaseOwner, "lease_owner", 160);
        b.Property(x => x.LeaseExpiresAtUtc).HasColumnName("lease_expires_at_utc"); b.Property(x => x.LastHeartbeatAtUtc).HasColumnName("last_heartbeat_at_utc"); b.Property(x => x.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc"); b.Property(x => x.AttemptCount).HasColumnName("attempt_count");
        Optional(b, x => x.ErrorCode, "error_code", 80); Optional(b, x => x.ErrorMessage, "error_message", 1000); b.Property(x => x.IsRecoverable).HasColumnName("is_recoverable");
        b.HasIndex(x => new { x.Status, x.NextAttemptAtUtc, x.LeaseExpiresAtUtc }).HasDatabaseName("ix_place_import_worker_queue");
        b.HasIndex(x => new { x.SourceId, x.TerritorialUnitId, x.CreatedAtUtc });
    }
    private static void TimeFields(EntityTypeBuilder<PlaceImportRunRecord> b) { b.Property(x=>x.CreatedAtUtc).HasColumnName("created_at_utc"); b.Property(x=>x.UpdatedAtUtc).HasColumnName("updated_at_utc"); b.Property(x=>x.ProcessingStartedAtUtc).HasColumnName("processing_started_at_utc"); b.Property(x=>x.ProcessingCompletedAtUtc).HasColumnName("processing_completed_at_utc"); }
    internal static void Id<T>(EntityTypeBuilder<T> b, System.Linq.Expressions.Expression<Func<T,Guid>> p) where T:class => b.Property(p).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
    internal static void Text<T>(EntityTypeBuilder<T> b, System.Linq.Expressions.Expression<Func<T,string>> p, string n, int l) where T:class => b.Property(p).HasColumnName(n).HasMaxLength(l).IsRequired();
    internal static void Optional<T>(EntityTypeBuilder<T> b, System.Linq.Expressions.Expression<Func<T,string?>> p, string n, int l) where T:class => b.Property(p).HasColumnName(n).HasMaxLength(l);
}

public sealed class PlaceSourceRecordConfiguration : IEntityTypeConfiguration<PlaceSourceRecord>
{
    public void Configure(EntityTypeBuilder<PlaceSourceRecord> b)
    {
        b.ToTable("place_source_records"); b.HasKey(x=>x.Id); PlaceImportRunConfiguration.Id(b,x=>x.Id);
        b.Property(x=>x.SourceId).HasColumnName("source_id"); b.HasOne(x=>x.Source).WithMany().HasForeignKey(x=>x.SourceId).OnDelete(DeleteBehavior.Restrict);
        PlaceImportRunConfiguration.Text(b,x=>x.ExternalId,"external_id",300); PlaceImportRunConfiguration.Optional(b,x=>x.SourceVersion,"source_version",120);
        b.Property(x=>x.NormalizedPayloadJson).HasColumnName("normalized_payload").HasColumnType("jsonb"); PlaceImportRunConfiguration.Text(b,x=>x.ContentHash,"content_hash",64);
        PlaceImportRunConfiguration.Text(b,x=>x.EvidenceUri,"evidence_uri",2048); b.Property(x=>x.ObservedAtUtc).HasColumnName("observed_at_utc"); b.Property(x=>x.RetrievedAtUtc).HasColumnName("retrieved_at_utc"); b.Property(x=>x.FirstSeenAtUtc).HasColumnName("first_seen_at_utc"); b.Property(x=>x.LastSeenAtUtc).HasColumnName("last_seen_at_utc");
        PlaceImportRunConfiguration.Text(b,x=>x.State,"state",24); b.Property(x=>x.CanonicalPlaceId).HasColumnName("canonical_place_id"); b.HasOne(x=>x.CanonicalPlace).WithMany().HasForeignKey(x=>x.CanonicalPlaceId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x=>new{x.SourceId,x.ExternalId}).IsUnique();
    }
}

public sealed class PlaceFieldObservationConfiguration : IEntityTypeConfiguration<PlaceFieldObservationRecord>
{
    public void Configure(EntityTypeBuilder<PlaceFieldObservationRecord> b)
    {
        b.ToTable("place_field_observations", t=>t.HasCheckConstraint("ck_place_field_authority","authority IN ('AUTO','MANUAL','MANUAL_VERIFIED')")); b.HasKey(x=>x.Id); PlaceImportRunConfiguration.Id(b,x=>x.Id);
        b.Property(x=>x.SourceRecordId).HasColumnName("source_record_id"); b.HasOne(x=>x.SourceRecord).WithMany().HasForeignKey(x=>x.SourceRecordId).OnDelete(DeleteBehavior.Cascade);
        b.Property(x=>x.ImportRunId).HasColumnName("import_run_id"); b.HasOne(x=>x.ImportRun).WithMany().HasForeignKey(x=>x.ImportRunId).OnDelete(DeleteBehavior.Cascade);
        b.Property(x=>x.PlaceId).HasColumnName("place_id"); b.HasOne(x=>x.Place).WithMany().HasForeignKey(x=>x.PlaceId).OnDelete(DeleteBehavior.SetNull);
        PlaceImportRunConfiguration.Text(b,x=>x.FieldName,"field_name",120); b.Property(x=>x.OriginalValue).HasColumnName("original_value").IsRequired(); b.Property(x=>x.NormalizedValue).HasColumnName("normalized_value").IsRequired();
        PlaceImportRunConfiguration.Text(b,x=>x.LicenseIdentifier,"license_identifier",80); PlaceImportRunConfiguration.Text(b,x=>x.Attribution,"attribution",500); PlaceImportRunConfiguration.Text(b,x=>x.Authority,"authority",24); PlaceImportRunConfiguration.Text(b,x=>x.State,"state",24);
        b.Property(x=>x.ObservedAtUtc).HasColumnName("observed_at_utc"); b.Property(x=>x.ValidUntilUtc).HasColumnName("valid_until_utc"); b.HasIndex(x=>new{x.SourceRecordId,x.FieldName,x.ObservedAtUtc});
    }
}

public sealed class PlaceImportItemConfiguration : IEntityTypeConfiguration<PlaceImportItemRecord>
{
    public void Configure(EntityTypeBuilder<PlaceImportItemRecord> b)
    {
        b.ToTable("place_import_items", t=>t.HasCheckConstraint("ck_place_import_item_coordinates","(latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL AND latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180)")); b.HasKey(x=>x.Id); PlaceImportRunConfiguration.Id(b,x=>x.Id);
        b.Property(x=>x.ImportRunId).HasColumnName("import_run_id"); b.HasOne(x=>x.ImportRun).WithMany().HasForeignKey(x=>x.ImportRunId).OnDelete(DeleteBehavior.Cascade);
        b.Property(x=>x.SourceRecordId).HasColumnName("source_record_id"); b.HasOne(x=>x.SourceRecord).WithMany().HasForeignKey(x=>x.SourceRecordId).OnDelete(DeleteBehavior.Restrict);
        PlaceImportRunConfiguration.Text(b,x=>x.Name,"name",200); PlaceImportRunConfiguration.Text(b,x=>x.Category,"category",80); b.Property(x=>x.Latitude).HasColumnName("latitude").HasPrecision(9,6); b.Property(x=>x.Longitude).HasColumnName("longitude").HasPrecision(9,6);
        b.Property(x=>x.NormalizedFieldsJson).HasColumnName("normalized_fields").HasColumnType("jsonb"); PlaceImportRunConfiguration.Text(b,x=>x.MatchBand,"match_band",40); b.Property(x=>x.MatchSignalsJson).HasColumnName("match_signals").HasColumnType("jsonb"); b.Property(x=>x.MatchContradictionsJson).HasColumnName("match_contradictions").HasColumnType("jsonb");
        b.Property(x=>x.PlaceId).HasColumnName("place_id"); b.HasOne(x=>x.Place).WithMany().HasForeignKey(x=>x.PlaceId).OnDelete(DeleteBehavior.SetNull); PlaceImportRunConfiguration.Text(b,x=>x.Status,"status",24); PlaceImportRunConfiguration.Text(b,x=>x.IdempotencyHash,"idempotency_hash",64);
        b.HasIndex(x=>new{x.ImportRunId,x.SourceRecordId}).IsUnique();
    }
}

public sealed class PlaceImportIssueConfiguration : IEntityTypeConfiguration<PlaceImportIssueRecord>
{
    public void Configure(EntityTypeBuilder<PlaceImportIssueRecord> b) { b.ToTable("place_import_issues"); b.HasKey(x=>x.Id); PlaceImportRunConfiguration.Id(b,x=>x.Id); b.Property(x=>x.ImportRunId).HasColumnName("import_run_id"); b.HasOne(x=>x.ImportRun).WithMany().HasForeignKey(x=>x.ImportRunId).OnDelete(DeleteBehavior.Cascade); PlaceImportRunConfiguration.Optional(b,x=>x.ExternalId,"external_id",300); PlaceImportRunConfiguration.Text(b,x=>x.Code,"code",80); PlaceImportRunConfiguration.Text(b,x=>x.Severity,"severity",20); b.Property(x=>x.Message).HasColumnName("message").HasMaxLength(1000).IsRequired(); b.Property(x=>x.CreatedAtUtc).HasColumnName("created_at_utc"); }
}

public sealed class PlaceImportDecisionConfiguration : IEntityTypeConfiguration<PlaceImportDecisionRecord>
{
    public void Configure(EntityTypeBuilder<PlaceImportDecisionRecord> b) { b.ToTable("place_import_decisions"); b.HasKey(x=>x.Id); PlaceImportRunConfiguration.Id(b,x=>x.Id); b.Property(x=>x.ImportRunId).HasColumnName("import_run_id"); b.HasOne(x=>x.ImportRun).WithMany().HasForeignKey(x=>x.ImportRunId).OnDelete(DeleteBehavior.Cascade); PlaceImportRunConfiguration.Text(b,x=>x.Action,"action",60); b.Property(x=>x.Reason).HasColumnName("reason").HasMaxLength(1000).IsRequired(); b.Property(x=>x.ActorId).HasColumnName("actor_id"); b.HasOne(x=>x.Actor).WithMany().HasForeignKey(x=>x.ActorId).OnDelete(DeleteBehavior.Restrict); b.Property(x=>x.DecidedAtUtc).HasColumnName("decided_at_utc"); b.HasIndex(x=>new{x.ImportRunId,x.DecidedAtUtc}); }
}
