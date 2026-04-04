using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Configurations;

public sealed class MenuRoleConfiguration : IEntityTypeConfiguration<MenuRoleRecord>
{
    public void Configure(EntityTypeBuilder<MenuRoleRecord> builder)
    {
        builder.ToTable("menu_roles");

        builder.HasKey(menuRole => menuRole.Id);

        builder.Property(menuRole => menuRole.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(menuRole => menuRole.MenuKey)
            .HasColumnName("menu_key")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(menuRole => menuRole.Role)
            .HasColumnName("role")
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(menuRole => new { menuRole.MenuKey, menuRole.Role })
            .IsUnique()
            .HasDatabaseName("uq_menu_roles_menu_key_role");

        builder.HasOne(menuRole => menuRole.Menu)
            .WithMany(menu => menu.MenuRoles)
            .HasForeignKey(menuRole => menuRole.MenuKey)
            .HasPrincipalKey(menu => menu.Key)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
