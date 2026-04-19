namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class MenuRoleRecord
{
    public Guid Id { get; set; }

    public string MenuKey { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public RoleRecord? RoleRef { get; set; }

    public MenuRecord? Menu { get; set; }
}
