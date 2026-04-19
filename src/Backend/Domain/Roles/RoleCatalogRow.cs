namespace YepPet.Domain.Roles;

public sealed record RoleCatalogRow(
    Guid Id,
    string Key,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
