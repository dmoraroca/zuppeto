using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class MissingPlaceProposalConfiguration : IEntityTypeConfiguration<MissingPlaceProposalRecord>
{
    public void Configure(EntityTypeBuilder<MissingPlaceProposalRecord> builder)
    {
        builder.ToTable("missing_place_proposals", table =>
            table.HasCheckConstraint("ck_missing_place_proposal_status", "status IN ('Pending','Accepted','Rejected','Duplicate')"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(item => item.SubmittedByUserId).HasColumnName("submitted_by_user_id");
        builder.HasOne<UserRecord>().WithMany().HasForeignKey(item => item.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(item => item.Municipality).HasColumnName("municipality").HasMaxLength(150).IsRequired();
        builder.Property(item => item.ApproximateAddress).HasColumnName("approximate_address").HasMaxLength(300).IsRequired();
        builder.Property(item => item.AdditionalInformation).HasColumnName("additional_information").HasMaxLength(2000);
        builder.Property(item => item.PossibleDuplicatePlaceId).HasColumnName("possible_duplicate_place_id");
        builder.HasOne<PlaceRecord>().WithMany().HasForeignKey(item => item.PossibleDuplicatePlaceId).OnDelete(DeleteBehavior.SetNull);
        builder.Property(item => item.Status).HasColumnName("status").HasMaxLength(24).IsRequired();
        builder.Property(item => item.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
        builder.HasOne<UserRecord>().WithMany().HasForeignKey(item => item.ReviewedByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.Property(item => item.ReviewReason).HasColumnName("review_reason").HasMaxLength(1000);
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("NOW()");
        builder.Property(item => item.ReviewedAtUtc).HasColumnName("reviewed_at_utc");
        builder.HasIndex(item => new { item.SubmittedByUserId, item.CreatedAtUtc }).HasDatabaseName("ix_missing_place_proposal_user_created");
        builder.HasIndex(item => new { item.Status, item.CreatedAtUtc }).HasDatabaseName("ix_missing_place_proposal_status_created");
    }
}
