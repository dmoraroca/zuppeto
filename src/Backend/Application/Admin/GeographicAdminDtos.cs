namespace Zuppeto.Application.Admin;

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
