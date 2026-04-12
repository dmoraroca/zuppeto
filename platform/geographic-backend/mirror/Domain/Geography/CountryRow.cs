namespace YepPet.Domain.Geography;

public sealed record CountryRow(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
