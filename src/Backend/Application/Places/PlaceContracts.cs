namespace Zuppeto.Application.Places;

public sealed record PlaceSearchRequest(
    string? SearchText,
    string? Country,
    string? City,
    string? Type,
    string PetCategory,
    int Skip = 0,
    int? Take = null,
    Guid? CountryId = null,
    Guid? TerritorialUnitId = null,
    IReadOnlyCollection<string>? Types = null,
    IReadOnlyCollection<string>? PetAccessStatuses = null,
    bool VerifiedOnly = false,
    string Sort = "Name",
    int RadiusKm = 0,
    decimal? CenterLatitude = null,
    decimal? CenterLongitude = null,
    decimal? North = null,
    decimal? South = null,
    decimal? East = null,
    decimal? West = null);

public sealed record PlaceSearchPageDto(
    IReadOnlyCollection<PlaceSummaryDto> Items,
    int Total,
    int Skip,
    int Take,
    bool HasMore,
    PlaceSearchMapDto? Map = null);

public sealed record PlaceSearchMapDto(
    decimal? CenterLatitude,
    decimal? CenterLongitude,
    decimal? MunicipalityLatitude,
    decimal? MunicipalityLongitude,
    int RadiusKm,
    IReadOnlyCollection<PlaceMapAreaDto> Areas,
    IReadOnlyCollection<PlaceMapPointDto>? Points = null);

public sealed record PlaceMapPointDto(
    Guid Id,
    string Name,
    string Type,
    string? CommercialCategory,
    string City,
    string Country,
    decimal Latitude,
    decimal Longitude,
    string PetAccessStatus,
    IReadOnlyCollection<string> PetSpecies,
    IReadOnlyCollection<PetConditionDto> PetConditions,
    bool IsPetPolicyVerified,
    decimal? DistanceKm,
    string BusinessStatus);

public sealed record PlaceMapAreaDto(
    Guid TerritorialUnitId,
    string Name,
    decimal? Latitude,
    decimal? Longitude,
    int PlaceCount);

public sealed record PlaceSearchHistoryDto(
    string SearchText,
    string City,
    string Type,
    string PetCategory,
    int HitCount,
    int ResultCount,
    DateTimeOffset LastRunAtUtc);

/// <summary>
/// Query for <c>GET /api/places/cities/search</c>: typeahead over place snapshots and published territorial localities.
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
    decimal? Latitude,
    decimal? Longitude,
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
    DateTimeOffset? LastGoogleSyncAt = null,
    Guid? CountryId = null,
    Guid? TerritorialUnitId = null,
    string PetAccessStatus = "Unknown",
    IReadOnlyCollection<string>? PetSpecies = null,
    IReadOnlyCollection<PetConditionDto>? PetConditions = null,
    string? CommercialCategory = null);

public sealed record PetConditionDto(string Code, string? Value = null);

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
    decimal? Latitude,
    decimal? Longitude,
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
    string? CategoryLabel = null,
    string? CoverAttribution = null,
    string? CoverSourceUri = null,
    Guid? CountryId = null,
    Guid? TerritorialUnitId = null,
    string PetAccessStatus = "Unknown",
    IReadOnlyCollection<string>? PetSpecies = null,
    IReadOnlyCollection<PetConditionDto>? PetConditions = null,
    decimal? DistanceKm = null,
    bool IsPetPolicyVerified = false,
    DateTimeOffset? PetPolicyVerifiedAtUtc = null,
    DateTimeOffset? UpdatedAtUtc = null,
    string BusinessStatus = "Open",
    IReadOnlyCollection<PlaceMediaAssetDto>? Media = null,
    string? CommercialCategory = null);

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
    decimal? Latitude,
    decimal? Longitude,
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
    string? CategoryLabel = null,
    Guid? CountryId = null,
    Guid? TerritorialUnitId = null,
    string PetAccessStatus = "Unknown",
    IReadOnlyCollection<string>? PetSpecies = null,
    IReadOnlyCollection<PetConditionDto>? PetConditions = null,
    bool IsPetPolicyVerified = false,
    DateTimeOffset? PetPolicyVerifiedAtUtc = null,
    DateTimeOffset? UpdatedAtUtc = null,
    string BusinessStatus = "Open",
    IReadOnlyCollection<PlaceMediaAssetDto>? Media = null,
    string? CommercialCategory = null,
    IReadOnlyCollection<PlaceDataAttributionDto>? DataAttributions = null);

public sealed record PlaceDataAttributionDto(
    string Organisation,
    string Dataset,
    string LicenseCode,
    string LicenseUrl,
    string Attribution,
    string DatasetUrl);

public interface IPlaceDataAttributionQuery
{
    Task<IReadOnlyCollection<PlaceDataAttributionDto>> ListAsync(
        Guid placeId,
        CancellationToken cancellationToken = default);
}

public sealed record PlacePublicSearchRow(
    Domain.Places.Place Place,
    decimal? DistanceKm,
    bool IsPetPolicyVerified,
    DateTimeOffset? PetPolicyVerifiedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string BusinessStatus);

public sealed record PlacePublicSearchResult(
    IReadOnlyCollection<PlacePublicSearchRow> Items,
    int Total,
    IReadOnlyCollection<PlaceMapAreaDto> Areas,
    decimal? CenterLatitude,
    decimal? CenterLongitude,
    decimal? MunicipalityLatitude,
    decimal? MunicipalityLongitude,
    IReadOnlyCollection<PlaceMapPointDto>? MapPoints = null);

public interface IPlacePublicSearchQuery
{
    Task<PlacePublicSearchResult> SearchAsync(PlaceSearchRequest request, CancellationToken cancellationToken = default);
}
