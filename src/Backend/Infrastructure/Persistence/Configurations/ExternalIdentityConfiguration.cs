using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class ExternalIdentityConfiguration : IEntityTypeConfiguration<ExternalIdentityRecord>
{
    public void Configure(EntityTypeBuilder<ExternalIdentityRecord> builder)
    {
        builder.ToTable("external_identities");
        builder.HasKey(identity => identity.Id);
        builder.Property(identity => identity.Id).HasColumnName("id");
        builder.Property(identity => identity.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(identity => identity.Provider).HasColumnName("provider").HasMaxLength(32).IsRequired();
        builder.Property(identity => identity.Subject).HasColumnName("subject").HasMaxLength(512).IsRequired();
        builder.Property(identity => identity.LinkedAtUtc).HasColumnName("linked_at_utc").IsRequired();
        builder.HasIndex(identity => new { identity.Provider, identity.Subject }).IsUnique().HasDatabaseName("uq_external_identities_provider_subject");
        builder.HasOne(identity => identity.User).WithMany(user => user.ExternalIdentities).HasForeignKey(identity => identity.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
