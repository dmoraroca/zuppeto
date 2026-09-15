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
            .HasMaxLength(512);

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

        builder.Property(user => user.EmailActivatedAtUtc)
            .HasColumnName("email_activated_at_utc");

        builder.Property(user => user.ActivationTokenHash)
            .HasColumnName("activation_token_hash")
            .HasMaxLength(128);

        builder.Property(user => user.ActivationTokenExpiresAtUtc)
            .HasColumnName("activation_token_expires_at_utc");

        builder.Property(user => user.ActivationTokenUsedAtUtc)
            .HasColumnName("activation_token_used_at_utc");

        builder.Property(user => user.PasswordResetTokenHash)
            .HasColumnName("password_reset_token_hash")
            .HasMaxLength(128);

        builder.Property(user => user.PasswordResetTokenExpiresAtUtc)
            .HasColumnName("password_reset_token_expires_at_utc");

        builder.Property(user => user.PasswordResetTokenUsedAtUtc)
            .HasColumnName("password_reset_token_used_at_utc");

        builder.Property(user => user.SecurityVersion)
            .HasColumnName("security_version")
            .HasDefaultValue(1)
            .IsRequired();
        builder.Property(user => user.TotpSecretProtected).HasColumnName("totp_secret_protected");
        builder.Property(user => user.PendingTotpSecretProtected).HasColumnName("pending_totp_secret_protected");
        builder.Property(user => user.PendingTotpExpiresAtUtc).HasColumnName("pending_totp_expires_at_utc");
        builder.Property(user => user.TotpEnabledAtUtc).HasColumnName("totp_enabled_at_utc");
        builder.Property(user => user.LastTotpTimeStepUsed).HasColumnName("last_totp_time_step_used");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("uq_users_email");

        builder.HasOne(user => user.FavoriteList)
            .WithOne(favoriteList => favoriteList.OwnerUser)
            .HasForeignKey<FavoriteListRecord>(favoriteList => favoriteList.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
