using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class TerritorialMappingTemplateConfiguration : IEntityTypeConfiguration<TerritorialMappingTemplateRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialMappingTemplateRecord> b)
    {
        b.ToTable("territorial_mapping_templates"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.DatasetSourceId).HasColumnName("dataset_source_id"); b.Property(x => x.Version).HasColumnName("version");
        b.Property(x => x.DefinitionJson).HasColumnName("definition_json").HasColumnType("jsonb");
        b.Property(x => x.SchemaFingerprint).HasColumnName("schema_fingerprint").HasMaxLength(64);
        b.Property(x => x.DefinitionChecksum).HasColumnName("definition_checksum").HasMaxLength(64);
        b.Property(x => x.IsActive).HasColumnName("is_active"); b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasOne(x => x.DatasetSource).WithMany().HasForeignKey(x => x.DatasetSourceId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.DatasetSourceId, x.Version }).IsUnique().HasDatabaseName("uq_territorial_mapping_source_version");
        b.HasIndex(x => new { x.DatasetSourceId, x.IsActive }).HasDatabaseName("ix_territorial_mapping_source_active");
    }
}

public sealed class TerritorialImportConfiguration : IEntityTypeConfiguration<TerritorialImportRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialImportRecord> b)
    {
        b.ToTable("territorial_imports", t =>
        {
            t.HasCheckConstraint("ck_territorial_import_status", "status IN ('Queued','Uploaded','Mapped','Validated','ReadyForReview','Publishing','Published','Failed','Cancelled','Reverted')");
            t.HasCheckConstraint("ck_territorial_import_size", "file_size > 0");
        });
        b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.DatasetSourceId).HasColumnName("dataset_source_id"); b.Property(x => x.MappingTemplateId).HasColumnName("mapping_template_id");
        b.Property(x => x.ArtifactName).HasColumnName("artifact_name").HasMaxLength(500); b.Property(x => x.ArtifactStorageKey).HasColumnName("artifact_storage_key").HasMaxLength(1000);
        b.Property(x => x.FileChecksum).HasColumnName("file_checksum").HasMaxLength(64); b.Property(x => x.FileSize).HasColumnName("file_size");
        b.Property(x => x.DatasetVersion).HasColumnName("dataset_version").HasMaxLength(120); b.Property(x => x.PublicationMode).HasColumnName("publication_mode").HasMaxLength(20);
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(24); b.Property(x => x.HasBlockingErrors).HasColumnName("has_blocking_errors");
        b.Property(x => x.CatalogVersion).HasColumnName("catalog_version"); b.Property(x => x.SummaryJson).HasColumnName("summary_json").HasColumnType("jsonb");
        b.Property(x => x.FailureReason).HasColumnName("failure_reason").HasMaxLength(2000); b.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        b.Property(x => x.PublicationRequestedByUserId).HasColumnName("publication_requested_by_user_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc"); b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc"); b.Property(x => x.PublishedAtUtc).HasColumnName("published_at_utc");
        b.Property(x => x.CurrentStage).HasColumnName("current_stage").HasMaxLength(32);
        b.Property(x => x.TotalRows).HasColumnName("total_rows"); b.Property(x => x.ProcessedRows).HasColumnName("processed_rows");
        b.Property(x => x.ProcessingStartedAtUtc).HasColumnName("processing_started_at_utc"); b.Property(x => x.ProcessingCompletedAtUtc).HasColumnName("processing_completed_at_utc");
        b.Property(x => x.LastHeartbeatAtUtc).HasColumnName("last_heartbeat_at_utc"); b.Property(x => x.AttemptCount).HasColumnName("attempt_count");
        b.Property(x => x.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(80); b.Property(x => x.LastErrorMessage).HasColumnName("last_error_message").HasMaxLength(1000);
        b.Property(x => x.IsRecoverable).HasColumnName("is_recoverable"); b.Property(x => x.CancellationRequested).HasColumnName("cancellation_requested");
        b.Property(x => x.LeaseOwner).HasColumnName("lease_owner").HasMaxLength(160); b.Property(x => x.LeaseExpiresAtUtc).HasColumnName("lease_expires_at_utc");
        b.Property(x => x.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc");
        b.HasOne(x => x.DatasetSource).WithMany().HasForeignKey(x => x.DatasetSourceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MappingTemplate).WithMany().HasForeignKey(x => x.MappingTemplateId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.DatasetSourceId, x.DatasetVersion, x.FileChecksum }).IsUnique().HasFilter("status = 'Published'").HasDatabaseName("uq_territorial_import_published_artifact");
        b.HasIndex(x => new { x.DatasetSourceId, x.CreatedAtUtc }).HasDatabaseName("ix_territorial_import_source_created");
        b.HasIndex(x => new { x.Status, x.NextAttemptAtUtc, x.LeaseExpiresAtUtc }).HasDatabaseName("ix_territorial_import_worker_queue");
    }
}

public sealed class TerritorialImportArtifactConfiguration : IEntityTypeConfiguration<TerritorialImportArtifactRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialImportArtifactRecord> b)
    {
        b.ToTable("territorial_import_artifacts");
        b.HasKey(x => x.ImportId);
        b.Property(x => x.ImportId).HasColumnName("import_id");
        b.Property(x => x.Content).HasColumnName("content").HasColumnType("bytea").IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        b.HasOne(x => x.Import).WithOne(x => x.Artifact).HasForeignKey<TerritorialImportArtifactRecord>(x => x.ImportId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TerritorialImportRowConfiguration : IEntityTypeConfiguration<TerritorialImportRowRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialImportRowRecord> b)
    {
        b.ToTable("territorial_import_rows"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.ImportId).HasColumnName("import_id"); b.Property(x => x.Sheet).HasColumnName("sheet").HasMaxLength(200); b.Property(x => x.RowNumber).HasColumnName("row_number");
        b.Property(x => x.SourceJson).HasColumnName("source_json").HasColumnType("jsonb"); b.Property(x => x.CanonicalJson).HasColumnName("canonical_json").HasColumnType("jsonb");
        b.Property(x => x.CanonicalUnitKey).HasColumnName("canonical_unit_key").HasMaxLength(300); b.Property(x => x.ParentCanonicalUnitKey).HasColumnName("parent_canonical_unit_key").HasMaxLength(300);
        b.HasOne(x => x.Import).WithMany(x => x.Rows).HasForeignKey(x => x.ImportId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.ImportId, x.CanonicalUnitKey }).HasDatabaseName("ix_territorial_import_rows_key");
    }
}

public sealed class TerritorialImportIssueConfiguration : IEntityTypeConfiguration<TerritorialImportIssueRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialImportIssueRecord> b)
    {
        b.ToTable("territorial_import_issues", t => t.HasCheckConstraint("ck_territorial_import_issue_severity", "severity IN ('Warning','Error')"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()"); b.Property(x => x.ImportId).HasColumnName("import_id");
        b.Property(x => x.RuleCode).HasColumnName("rule_code").HasMaxLength(80); b.Property(x => x.Severity).HasColumnName("severity").HasMaxLength(10); b.Property(x => x.Message).HasColumnName("message").HasMaxLength(1000);
        b.Property(x => x.Sheet).HasColumnName("sheet").HasMaxLength(200); b.Property(x => x.RowNumber).HasColumnName("row_number"); b.Property(x => x.Field).HasColumnName("field").HasMaxLength(160);
        b.Property(x => x.ProblemValue).HasColumnName("problem_value").HasMaxLength(500); b.Property(x => x.CanonicalUnitKey).HasColumnName("canonical_unit_key").HasMaxLength(300); b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasOne(x => x.Import).WithMany(x => x.Issues).HasForeignKey(x => x.ImportId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.ImportId, x.Severity }).HasDatabaseName("ix_territorial_import_issues_severity");
    }
}

public sealed class TerritorialCatalogStateConfiguration : IEntityTypeConfiguration<TerritorialCatalogStateRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialCatalogStateRecord> b)
    {
        b.ToTable("territorial_catalog_states", t => t.HasCheckConstraint("ck_territorial_catalog_version", "version >= 0")); b.HasKey(x => x.CountryId);
        b.Property(x => x.CountryId).HasColumnName("country_id"); b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken(); b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.Country).WithOne().HasForeignKey<TerritorialCatalogStateRecord>(x => x.CountryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TerritorialChangeSetConfiguration : IEntityTypeConfiguration<TerritorialChangeSetRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialChangeSetRecord> b)
    {
        b.ToTable("territorial_change_sets", t => t.HasCheckConstraint("ck_territorial_change_set_status", "status IN ('Prepared','Published','Reverted')")); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()"); b.Property(x => x.ImportId).HasColumnName("import_id"); b.Property(x => x.CountryId).HasColumnName("country_id");
        b.Property(x => x.CatalogVersion).HasColumnName("catalog_version"); b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20); b.Property(x => x.RevertsChangeSetId).HasColumnName("reverts_change_set_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc"); b.Property(x => x.PublishedAtUtc).HasColumnName("published_at_utc");
        b.HasOne(x => x.Import).WithMany(x => x.ChangeSets).HasForeignKey(x => x.ImportId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Country).WithMany().HasForeignKey(x => x.CountryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RevertsChangeSet).WithMany().HasForeignKey(x => x.RevertsChangeSetId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CountryId, x.CatalogVersion }).HasDatabaseName("ix_territorial_change_set_country_version");
    }
}

public sealed class TerritorialChangeSetItemConfiguration : IEntityTypeConfiguration<TerritorialChangeSetItemRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialChangeSetItemRecord> b)
    {
        b.ToTable("territorial_change_set_items", t => t.HasCheckConstraint("ck_territorial_change_kind", "kind IN ('Create','Update','Deactivate','NoChange')")); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()"); b.Property(x => x.ChangeSetId).HasColumnName("change_set_id"); b.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(20);
        b.Property(x => x.TerritorialUnitId).HasColumnName("territorial_unit_id"); b.Property(x => x.CanonicalUnitKey).HasColumnName("canonical_unit_key").HasMaxLength(300);
        b.Property(x => x.BeforeJson).HasColumnName("before_json").HasColumnType("jsonb"); b.Property(x => x.AfterJson).HasColumnName("after_json").HasColumnType("jsonb"); b.Property(x => x.ChangedFieldsJson).HasColumnName("changed_fields_json").HasColumnType("jsonb");
        b.HasOne(x => x.ChangeSet).WithMany(x => x.Items).HasForeignKey(x => x.ChangeSetId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.ChangeSetId, x.CanonicalUnitKey }).HasDatabaseName("ix_territorial_change_items_key");
    }
}
