using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

internal sealed class ExternalIntelligenceRequestConfiguration : IEntityTypeConfiguration<ExternalIntelligenceRequestRecord>
{
    public void Configure(EntityTypeBuilder<ExternalIntelligenceRequestRecord> b)
    {
        b.ToTable("ExternalIntelligenceRequests"); b.HasKey(x => x.Id);
        b.Property(x => x.Provider).HasMaxLength(40).IsRequired(); b.Property(x => x.Api).HasMaxLength(30).IsRequired();
        b.Property(x => x.Model).HasMaxLength(120).IsRequired(); b.Property(x => x.Operation).HasMaxLength(100).IsRequired();
        b.Property(x => x.PromptVersion).HasMaxLength(80).IsRequired(); b.Property(x => x.QueryHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.NormalizedRequestJson).HasColumnType("jsonb").IsRequired(); b.Property(x => x.ResponseJson).HasColumnType("jsonb");
        b.Property(x => x.CategoriesJson).HasColumnType("jsonb").IsRequired(); b.Property(x => x.ToolsJson).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.SourcesJson).HasColumnType("jsonb").IsRequired(); b.Property(x => x.Status).HasMaxLength(30).IsRequired();
        b.Property(x => x.FunctionalStatus).HasMaxLength(80); b.Property(x => x.VerificationStatus).HasMaxLength(30).IsRequired();
        b.Property(x => x.EstimatedCostUsd).HasPrecision(12, 6); b.Property(x => x.ActualCostUsd).HasPrecision(12, 6);
        b.Property(x => x.ProviderRequestId).HasMaxLength(160); b.Property(x => x.ErrorCode).HasMaxLength(100); b.Property(x => x.BlockReason).HasMaxLength(100);
        b.HasIndex(x => new { x.QueryHash, x.CreatedAtUtc }); b.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        b.HasIndex(x => x.ProcessId); b.HasIndex(x => x.MunicipalityId);
        b.HasOne<TerritorialUnitRecord>().WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.PlaceId); b.HasOne<PlaceRecord>().WithMany().HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ExternalIntelligenceCacheConfiguration : IEntityTypeConfiguration<ExternalIntelligenceCacheRecord>
{
    public void Configure(EntityTypeBuilder<ExternalIntelligenceCacheRecord> b)
    {
        b.ToTable("ExternalIntelligenceCache"); b.HasKey(x => x.QueryHash); b.Property(x => x.QueryHash).HasMaxLength(64);
        b.Property(x => x.Status).HasMaxLength(30).IsRequired(); b.Property(x => x.ResponseJson).HasColumnType("jsonb");
        b.Property(x => x.SourcesJson).HasColumnType("jsonb").IsRequired(); b.Property(x => x.LastCostUsd).HasPrecision(12, 6);
        b.HasIndex(x => x.ValidUntilUtc);
    }
}

internal sealed class ExternalIntelligenceEvidenceConfiguration : IEntityTypeConfiguration<ExternalIntelligenceEvidenceRecord>
{
    public void Configure(EntityTypeBuilder<ExternalIntelligenceEvidenceRecord> b)
    {
        b.ToTable("ExternalIntelligenceEvidence"); b.HasKey(x => x.Id);
        b.Property(x => x.FieldName).HasMaxLength(120).IsRequired(); b.Property(x => x.ProposedValueJson).HasColumnType("jsonb");
        b.Property(x => x.SourceUrl).HasMaxLength(2048).IsRequired(); b.Property(x => x.SourceType).HasMaxLength(80).IsRequired();
        b.Property(x => x.VerificationStatus).HasMaxLength(30).IsRequired(); b.Property(x => x.AdminReviewStatus).HasMaxLength(30).IsRequired();
        b.Property(x => x.LicenseUrl).HasMaxLength(2048); b.HasIndex(x => x.RequestId); b.HasIndex(x => x.PlaceId);
        b.HasOne<ExternalIntelligenceRequestRecord>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PlaceRecord>().WithMany().HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class ExternalIntelligenceSearchConfiguration : IEntityTypeConfiguration<ExternalIntelligenceSearchRecord>
{
    public void Configure(EntityTypeBuilder<ExternalIntelligenceSearchRecord> b)
    {
        b.ToTable("ExternalIntelligenceSearches"); b.HasKey(x => x.Id); b.Property(x => x.ViewerHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.QueryHash).HasMaxLength(64).IsRequired(); b.Property(x => x.CategoriesJson).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.FiltersJson).HasColumnType("jsonb").IsRequired(); b.Property(x => x.DataOrigin).HasMaxLength(50).IsRequired();
        b.Property(x => x.AttributedCostUsd).HasPrecision(12, 6); b.HasIndex(x => x.CreatedAtUtc); b.HasIndex(x => x.QueryHash);
        b.HasOne<ExternalIntelligenceRequestRecord>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<TerritorialUnitRecord>().WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ExternalIntelligenceBudgetConfiguration : IEntityTypeConfiguration<ExternalIntelligenceBudgetRecord>
{
    public void Configure(EntityTypeBuilder<ExternalIntelligenceBudgetRecord> b)
    {
        b.ToTable("ExternalIntelligenceBudget"); b.HasKey(x => x.Id); b.Property(x => x.DailyLimitUsd).HasPrecision(12, 6);
        b.Property(x => x.MonthlyLimitUsd).HasPrecision(12, 6); b.Property(x => x.MaximumRequestUsd).HasPrecision(12, 6);
        b.Property(x => x.AlertThresholdsJson).HasColumnType("jsonb").IsRequired();
    }
}
