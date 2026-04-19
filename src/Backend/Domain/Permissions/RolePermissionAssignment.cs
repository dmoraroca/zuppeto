namespace YepPet.Domain.Permissions;

public sealed record RolePermissionAssignment(
    string Role,
    string PermissionKey);
