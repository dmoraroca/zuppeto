namespace YepPet.Domain.Permissions;

public sealed record PermissionDefinition(
    string Key,
    string ScopeType,
    string DisplayName,
    string Description,
    string? ScopePayload,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
