namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class RoleRecord
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<UserRecord> Users { get; set; } = [];

    public ICollection<RolePermissionRecord> RolePermissions { get; set; } = [];

    public ICollection<MenuRoleRecord> MenuRoles { get; set; } = [];
}
