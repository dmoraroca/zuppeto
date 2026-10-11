using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class TerritorialSourceApprovalAuditConfiguration : IEntityTypeConfiguration<TerritorialSourceApprovalAuditRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialSourceApprovalAuditRecord> builder)
    {
        builder.ToTable("territorial_source_approval_audit", table =>
        {
            table.HasCheckConstraint("ck_territorial_source_approval_previous_status",
                "previous_status IN ('Pending', 'Approved', 'Rejected')");
            table.HasCheckConstraint("ck_territorial_source_approval_new_status",
                "new_status IN ('Approved', 'Rejected')");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(item => item.DatasetSourceId).HasColumnName("dataset_source_id");
        builder.Property(item => item.PreviousStatus).HasColumnName("previous_status").HasMaxLength(20).IsRequired();
        builder.Property(item => item.NewStatus).HasColumnName("new_status").HasMaxLength(20).IsRequired();
        builder.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
        builder.Property(item => item.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.HasOne(item => item.DatasetSource).WithMany(source => source.ApprovalHistory)
            .HasForeignKey(item => item.DatasetSourceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ActorUser).WithMany()
            .HasForeignKey(item => item.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.DatasetSourceId, item.CreatedAtUtc })
            .HasDatabaseName("ix_territorial_source_approval_source_created");
    }
}
