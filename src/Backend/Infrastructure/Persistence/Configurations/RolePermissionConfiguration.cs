using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermissionRecord>
{
    public void Configure(EntityTypeBuilder<RolePermissionRecord> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(rolePermission => rolePermission.Id);

        builder.Property(rolePermission => rolePermission.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(rolePermission => rolePermission.Role)
            .HasColumnName("role")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(rolePermission => rolePermission.PermissionKey)
            .HasColumnName("permission_key")
            .HasMaxLength(160)
            .IsRequired();

        builder.HasIndex(rolePermission => new { rolePermission.Role, rolePermission.PermissionKey })
            .IsUnique()
            .HasDatabaseName("uq_role_permissions_role_permission_key");

        builder.HasOne(rolePermission => rolePermission.Permission)
            .WithMany(permission => permission.RolePermissions)
            .HasForeignKey(rolePermission => rolePermission.PermissionKey)
            .HasPrincipalKey(permission => permission.Key)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
