using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Configurations;

public sealed class PlaceReviewConfiguration : IEntityTypeConfiguration<PlaceReviewRecord>
{
    public void Configure(EntityTypeBuilder<PlaceReviewRecord> builder)
    {
        builder.ToTable("place_reviews");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(record => record.PlaceId)
            .HasColumnName("place_id")
            .IsRequired();

        builder.Property(record => record.AuthorUserId)
            .HasColumnName("author_user_id")
            .IsRequired();

        builder.Property(record => record.Score)
            .HasColumnName("score")
            .IsRequired();

        builder.Property(record => record.Comment)
            .HasColumnName("comment")
            .IsRequired();

        builder.Property(record => record.IsVisible)
            .HasColumnName("is_visible")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(record => record.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(record => record.PlaceId)
            .HasDatabaseName("ix_place_reviews_place_id");

        builder.HasOne(record => record.Place)
            .WithMany(place => place.Reviews)
            .HasForeignKey(record => record.PlaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(record => record.AuthorUser)
            .WithMany(user => user.Reviews)
            .HasForeignKey(record => record.AuthorUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_place_reviews_score", "score >= 1 AND score <= 5");
        });
    }
}
