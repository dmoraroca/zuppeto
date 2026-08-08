using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Configurations;

public sealed class PlaceConfiguration : IEntityTypeConfiguration<PlaceRecord>
{
    public void Configure(EntityTypeBuilder<PlaceRecord> builder)
    {
        builder.ToTable("places");

        builder.HasKey(place => place.Id);

        builder.Property(place => place.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(place => place.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(place => place.Type)
            .HasColumnName("type")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(place => place.ShortDescription)
            .HasColumnName("short_description")
            .IsRequired();

        builder.Property(place => place.Description)
            .HasColumnName("description")
            .IsRequired();

        builder.Property(place => place.CoverImageUrl)
            .HasColumnName("cover_image_url")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(place => place.AddressLine1)
            .HasColumnName("address_line1")
            .HasMaxLength(240)
            .IsRequired();

        builder.Property(place => place.City)
            .HasColumnName("city")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(place => place.Country)
            .HasColumnName("country")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(place => place.Neighborhood)
            .HasColumnName("neighborhood")
            .HasMaxLength(120);

        builder.Property(place => place.Latitude)
            .HasColumnName("latitude")
            .HasPrecision(9, 6);

        builder.Property(place => place.Longitude)
            .HasColumnName("longitude")
            .HasPrecision(9, 6);

        builder.Property(place => place.ExcludeFromOsmMap)
            .HasColumnName("exclude_from_osm_map")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(place => place.AcceptsDogs)
            .HasColumnName("accepts_dogs")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(place => place.AcceptsCats)
            .HasColumnName("accepts_cats")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(place => place.PetPolicyLabel)
            .HasColumnName("pet_policy_label")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(place => place.PetPolicyNotes)
            .HasColumnName("pet_policy_notes");

        builder.Property(place => place.PricingLabel)
            .HasColumnName("pricing_label")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(place => place.RatingAverage)
            .HasColumnName("rating_average")
            .HasPrecision(3, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(place => place.ReviewCount)
            .HasColumnName("review_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(place => place.DataProvenance)
            .HasColumnName("data_provenance")
            .HasMaxLength(40)
            .HasDefaultValue("Internal")
            .IsRequired();

        builder.Property(place => place.GooglePlaceId)
            .HasColumnName("google_place_id")
            .HasMaxLength(256);

        builder.Property(place => place.GoogleCoordinatesCachedUntil)
            .HasColumnName("google_coordinates_cached_until");

        builder.Property(place => place.LastGoogleSyncAt)
            .HasColumnName("last_google_sync_at");

        builder.Property(place => place.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(place => place.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasIndex(place => place.GooglePlaceId)
            .IsUnique()
            .HasDatabaseName("ux_places_google_place_id")
            .HasFilter("google_place_id IS NOT NULL");

        builder.HasIndex(place => place.City)
            .HasDatabaseName("ix_places_city");

        builder.HasIndex(place => place.Type)
            .HasDatabaseName("ix_places_type");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_places_pet_policy", "accepts_dogs OR accepts_cats");
            table.HasCheckConstraint("ck_places_rating_average", "rating_average >= 0 AND rating_average <= 5");
            table.HasCheckConstraint("ck_places_review_count", "review_count >= 0");
        });
    }
}
