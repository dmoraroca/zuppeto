namespace YepPet.Domain.Geography;

public sealed record CityRow(
    Guid Id,
    Guid CountryId,
    string CountryName,
    string CountryCode,
    string Name,
    string NormalizedName,
    decimal? Latitude,
    decimal? Longitude,
    bool IsActive,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
