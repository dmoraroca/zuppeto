using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<UserRecord>
{
    public void Configure(EntityTypeBuilder<UserRecord> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasColumnName("role")
            .HasMaxLength(32)
            .IsRequired();

        builder.HasOne(user => user.RoleRef)
            .WithMany(role => role.Users)
            .HasForeignKey(user => user.Role)
            .HasPrincipalKey(role => role.Key)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(user => user.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200);

        builder.Property(user => user.City)
            .HasColumnName("city")
            .HasMaxLength(120);

        builder.Property(user => user.Country)
            .HasColumnName("country")
            .HasMaxLength(120);

        builder.Property(user => user.Comments)
            .HasColumnName("comments");

        builder.Property(user => user.AvatarUrl)
            .HasColumnName("avatar_url");

        builder.Property(user => user.PrivacyAccepted)
            .HasColumnName("privacy_accepted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(user => user.PrivacyAcceptedAtUtc)
            .HasColumnName("privacy_accepted_at_utc");

        builder.Property(user => user.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(user => user.LastAccessedAtUtc)
            .HasColumnName("last_accessed_at_utc");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("uq_users_email");

        builder.HasOne(user => user.FavoriteList)
            .WithOne(favoriteList => favoriteList.OwnerUser)
            .HasForeignKey<FavoriteListRecord>(favoriteList => favoriteList.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
