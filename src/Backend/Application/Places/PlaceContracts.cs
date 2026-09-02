namespace Zuppeto.Application.Places;

public sealed record PlaceSearchRequest(
    string? SearchText,
    string? City,
    string? Type,
    string PetCategory,
    int Skip = 0,
    int? Take = null);

public sealed record PlaceSearchPageDto(
    IReadOnlyCollection<PlaceSummaryDto> Items,
    int Total,
    int Skip,
    int Take,
    bool HasMore);

public sealed record PlaceSearchHistoryDto(
    string SearchText,
    string City,
    string Type,
    string PetCategory,
    int HitCount,
    int ResultCount,
    DateTimeOffset LastRunAtUtc);

public sealed record PlaceExternalSearchRequest(
    string? Query,
    string? City,
    string? Type,
    int? Limit);

public sealed record PlaceExternalCandidateDto(
    string Name,
    string Address,
    string City,
    string Country,
    decimal Latitude,
    decimal Longitude,
    string ExternalId,
    string Source,
    bool? PetFriendlyAuto,
    string? PhotoReference = null);

/// <summary>
/// Query for <c>GET /api/places/cities/search</c>: typeahead over distinct cities that have at least one place.
/// </summary>
public sealed record PlaceCitySearchRequest(string? Q, int? Limit);

public sealed record PlaceCitySuggestionDto(
    string City,
    string Country,
    string? CountryCode,
    string DisplayLabel,
    string Source);

public sealed record PlaceUpsertRequest(
    Guid? Id,
    string Name,
    string Type,
    string ShortDescription,
    string Description,
    string CoverImageUrl,
    string AddressLine1,
    string City,
    string Country,
    string Neighborhood,
    decimal Latitude,
    decimal Longitude,
    bool AcceptsDogs,
    bool AcceptsCats,
    string PetPolicyLabel,
    string PetPolicyNotes,
    string PricingLabel,
    decimal RatingAverage,
    int ReviewCount,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> Features,
    string? DataProvenance = null,
    string? GooglePlaceId = null,
    DateTimeOffset? GoogleCoordinatesCachedUntil = null,
    DateTimeOffset? LastGoogleSyncAt = null);

public sealed record PlaceSummaryDto(
    Guid Id,
    string Name,
    string Type,
    string ShortDescription,
    string Description,
    string CoverImageUrl,
    string AddressLine1,
    string City,
    string Country,
    string Neighborhood,
    decimal Latitude,
    decimal Longitude,
    string DataProvenance,
    string? GooglePlaceId,
    DateTimeOffset? GoogleCoordinatesCachedUntil,
    DateTimeOffset? LastGoogleSyncAt,
    bool GoogleCoordinatesCacheExpired,
    bool RequiresGoogleMapForGoogleCoordinates,
    bool ExcludeFromOsmMap,
    bool AcceptsDogs,
    bool AcceptsCats,
    string PetPolicyLabel,
    string PetPolicyNotes,
    string PricingLabel,
    decimal RatingAverage,
    int ReviewCount,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> Features,
    string? OpeningHours = null,
    string? Phone = null,
    string? Website = null,
    string? CategoryLabel = null);

public sealed record PlaceDetailDto(
    Guid Id,
    string Name,
    string Type,
    string ShortDescription,
    string Description,
    string CoverImageUrl,
    string AddressLine1,
    string City,
    string Country,
    string Neighborhood,
    decimal Latitude,
    decimal Longitude,
    string DataProvenance,
    string? GooglePlaceId,
    DateTimeOffset? GoogleCoordinatesCachedUntil,
    DateTimeOffset? LastGoogleSyncAt,
    bool GoogleCoordinatesCacheExpired,
    bool RequiresGoogleMapForGoogleCoordinates,
    bool ExcludeFromOsmMap,
    bool AcceptsDogs,
    bool AcceptsCats,
    string PetPolicyLabel,
    string PetPolicyNotes,
    string PricingLabel,
    decimal RatingAverage,
    int ReviewCount,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> Features,
    string? CoverAttribution = null,
    string? CoverSourceUri = null,
    string? OpeningHours = null,
    string? Phone = null,
    string? Website = null,
    string? CategoryLabel = null);
