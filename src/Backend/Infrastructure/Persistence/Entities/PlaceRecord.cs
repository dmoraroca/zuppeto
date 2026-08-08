namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class PlaceRecord
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CoverImageUrl { get; set; } = string.Empty;

    public string AddressLine1 { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string? Neighborhood { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public bool ExcludeFromOsmMap { get; set; }

    public bool AcceptsDogs { get; set; }

    public bool AcceptsCats { get; set; }

    public string PetPolicyLabel { get; set; } = string.Empty;

    public string? PetPolicyNotes { get; set; }

    public string PricingLabel { get; set; } = string.Empty;

    public decimal RatingAverage { get; set; }

    public int ReviewCount { get; set; }

    public string DataProvenance { get; set; } = "Internal";

    public string? GooglePlaceId { get; set; }

    public DateTimeOffset? GoogleCoordinatesCachedUntil { get; set; }

    public DateTimeOffset? LastGoogleSyncAt { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<FavoriteEntryRecord> FavoriteEntries { get; set; } = [];

    public ICollection<PlaceFeatureRecord> PlaceFeatures { get; set; } = [];

    public ICollection<PlaceReviewRecord> Reviews { get; set; } = [];

    public ICollection<PlaceTagRecord> PlaceTags { get; set; } = [];
}
