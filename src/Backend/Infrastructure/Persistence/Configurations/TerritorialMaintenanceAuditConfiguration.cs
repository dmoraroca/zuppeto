using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class TerritorialMaintenanceAuditConfiguration : IEntityTypeConfiguration<TerritorialMaintenanceAuditRecord>
{
    public void Configure(EntityTypeBuilder<TerritorialMaintenanceAuditRecord> b)
    {
        b.ToTable("territorial_maintenance_audit", t =>
            t.HasCheckConstraint("ck_territorial_maintenance_origin", "origin IN ('Manual','Import')"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.TerritorialUnitId).HasColumnName("territorial_unit_id");
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(60);
        b.Property(x => x.Field).HasColumnName("field").HasMaxLength(80);
        b.Property(x => x.BeforeValue).HasColumnName("before_value").HasColumnType("jsonb");
        b.Property(x => x.AfterValue).HasColumnName("after_value").HasColumnType("jsonb");
        b.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);
        b.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        b.Property(x => x.Origin).HasColumnName("origin").HasMaxLength(20);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasOne(x => x.TerritorialUnit).WithMany(x => x.MaintenanceAudit).HasForeignKey(x => x.TerritorialUnitId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TerritorialUnitId, x.CreatedAtUtc }).HasDatabaseName("ix_territorial_maintenance_unit_created");
    }
}
