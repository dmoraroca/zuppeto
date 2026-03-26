using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Configurations;

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

        builder.Property(user => user.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200);

        builder.Property(user => user.City)
            .HasColumnName("city")
            .HasMaxLength(120);

        builder.Property(user => user.Country)
            .HasColumnName("country")
            .HasMaxLength(120);

        builder.Property(user => user.Bio)
            .HasColumnName("bio");

        builder.Property(user => user.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(2048);

        builder.Property(user => user.PrivacyAccepted)
            .HasColumnName("privacy_accepted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(user => user.PrivacyAcceptedAtUtc)
            .HasColumnName("privacy_accepted_at_utc");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("uq_users_email");

        builder.HasOne(user => user.FavoriteList)
            .WithOne(favoriteList => favoriteList.OwnerUser)
            .HasForeignKey<FavoriteListRecord>(favoriteList => favoriteList.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
