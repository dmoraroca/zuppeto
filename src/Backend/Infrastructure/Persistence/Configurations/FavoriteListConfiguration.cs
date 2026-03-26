using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Configurations;

public sealed class FavoriteListConfiguration : IEntityTypeConfiguration<FavoriteListRecord>
{
    public void Configure(EntityTypeBuilder<FavoriteListRecord> builder)
    {
        builder.ToTable("favorite_lists");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(record => record.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        builder.HasIndex(record => record.OwnerUserId)
            .IsUnique()
            .HasDatabaseName("uq_favorite_lists_owner_user_id");
    }
}
