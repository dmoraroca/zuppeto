namespace Zuppeto.Application.Places;

public static class PetGovernanceAuthorizationPolicy
{
    public static bool Allows(Guid? actorId, string? roleKey, bool hasPlacesManagePermission) =>
        actorId.HasValue
        && string.Equals(roleKey, "Admin", StringComparison.OrdinalIgnoreCase)
        && hasPlacesManagePermission;
}
