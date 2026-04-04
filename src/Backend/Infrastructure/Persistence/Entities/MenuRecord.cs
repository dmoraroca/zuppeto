namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class MenuRecord
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? Route { get; set; }

    public string? ParentKey { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public ICollection<MenuRoleRecord> MenuRoles { get; set; } = [];
}
