namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class PermissionRecord
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string ScopeType { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ICollection<RolePermissionRecord> RolePermissions { get; set; } = [];
}
