namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class RolePermissionRecord
{
    public Guid Id { get; set; }

    public string Role { get; set; } = string.Empty;

    public string PermissionKey { get; set; } = string.Empty;

    public PermissionRecord? Permission { get; set; }
}
