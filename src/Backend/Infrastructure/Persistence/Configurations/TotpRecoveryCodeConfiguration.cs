using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class TotpRecoveryCodeConfiguration : IEntityTypeConfiguration<TotpRecoveryCodeRecord>
{
    public void Configure(EntityTypeBuilder<TotpRecoveryCodeRecord> builder)
    {
        builder.ToTable("totp_recovery_codes");
        builder.HasKey(code => code.Id);
        builder.Property(code => code.Id).HasColumnName("id");
        builder.Property(code => code.UserId).HasColumnName("user_id");
        builder.Property(code => code.CodeHash).HasColumnName("code_hash").HasMaxLength(128).IsRequired();
        builder.Property(code => code.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(code => code.UsedAtUtc).HasColumnName("used_at_utc");
        builder.HasIndex(code => new { code.UserId, code.CodeHash }).IsUnique();
        builder.HasIndex(code => code.UserId);
        builder.HasOne<UserRecord>().WithMany().HasForeignKey(code => code.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
