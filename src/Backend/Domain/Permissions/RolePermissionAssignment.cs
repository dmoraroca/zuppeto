using YepPet.Domain.Users;

namespace YepPet.Domain.Permissions;

public sealed record RolePermissionAssignment(
    UserRole Role,
    string PermissionKey);
