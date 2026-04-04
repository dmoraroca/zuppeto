namespace YepPet.Domain.Navigation;

public sealed record MenuItemDefinition(
    string Key,
    string Label,
    string? Route,
    string? ParentKey,
    int SortOrder,
    bool IsActive);
