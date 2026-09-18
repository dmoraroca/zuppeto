using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class RevokedAccessTokenConfiguration : IEntityTypeConfiguration<RevokedAccessTokenRecord>
{
    public void Configure(EntityTypeBuilder<RevokedAccessTokenRecord> builder)
    {
        builder.ToTable("revoked_access_tokens");
        builder.HasKey(token => token.TokenId);
        builder.Property(token => token.TokenId).HasColumnName("token_id").HasMaxLength(64);
        builder.Property(token => token.UserId).HasColumnName("user_id");
        builder.Property(token => token.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(token => token.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.HasIndex(token => token.ExpiresAtUtc);
        builder.HasIndex(token => token.UserId);
        builder.HasOne<UserRecord>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
