using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Configurations;

public sealed class MenuConfiguration : IEntityTypeConfiguration<MenuRecord>
{
    public void Configure(EntityTypeBuilder<MenuRecord> builder)
    {
        builder.ToTable("menus");

        builder.HasKey(menu => menu.Id);

        builder.Property(menu => menu.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(menu => menu.Key)
            .HasColumnName("key")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(menu => menu.Label)
            .HasColumnName("label")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(menu => menu.Route)
            .HasColumnName("route")
            .HasMaxLength(256);

        builder.Property(menu => menu.ParentKey)
            .HasColumnName("parent_key")
            .HasMaxLength(160);

        builder.Property(menu => menu.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(menu => menu.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(menu => menu.Key)
            .IsUnique()
            .HasDatabaseName("uq_menus_key");

        builder.HasOne<MenuRecord>()
            .WithMany()
            .HasForeignKey(menu => menu.ParentKey)
            .HasPrincipalKey(menu => menu.Key)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
