using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Configurations;

public sealed class PrivacyConsentEventConfiguration : IEntityTypeConfiguration<PrivacyConsentEventRecord>
{
    public void Configure(EntityTypeBuilder<PrivacyConsentEventRecord> builder)
    {
        builder.ToTable("privacy_consent_events");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(record => record.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(record => record.Accepted)
            .HasColumnName("accepted")
            .IsRequired();

        builder.Property(record => record.RegisteredAtUtc)
            .HasColumnName("registered_at_utc")
            .IsRequired();

        builder.Property(record => record.Source)
            .HasColumnName("source")
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(record => record.UserId)
            .HasDatabaseName("ix_privacy_consent_events_user_id");

        builder.HasOne(record => record.User)
            .WithMany(user => user.PrivacyConsentEvents)
            .HasForeignKey(record => record.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
