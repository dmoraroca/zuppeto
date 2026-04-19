using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<PermissionRecord>
{
    public void Configure(EntityTypeBuilder<PermissionRecord> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(permission => permission.Key)
            .HasColumnName("key")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(permission => permission.ScopeType)
            .HasColumnName("scope_type")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(permission => permission.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(permission => permission.Description)
            .HasColumnName("description")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(permission => permission.ScopePayload)
            .HasColumnName("scope_payload")
            .HasColumnType("text");

        builder.Property(permission => permission.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(permission => permission.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(permission => permission.Key)
            .IsUnique()
            .HasDatabaseName("uq_permissions_key");
    }
}
