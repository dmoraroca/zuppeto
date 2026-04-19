namespace YepPet.Application.Admin;

public sealed record CountryAdminDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateCountryRequest(string Code, string Name, bool IsActive, int SortOrder);

public sealed record UpdateCountryRequest(string Code, string Name, bool IsActive, int SortOrder);

public sealed record CityAdminDto(
    Guid Id,
    Guid CountryId,
    string CountryName,
    string CountryCode,
    string Name,
    decimal? Latitude,
    decimal? Longitude,
    bool IsActive,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateCityRequest(Guid CountryId, string Name, decimal? Latitude, decimal? Longitude, bool IsActive, int SortOrder);

public sealed record UpdateCityRequest(string Name, decimal? Latitude, decimal? Longitude, bool IsActive, int SortOrder);
