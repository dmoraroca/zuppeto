namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class UserRecord
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public RoleRecord? RoleRef { get; set; }

    public string? DisplayName { get; set; }

    public string? City { get; set; }

    public string? Country { get; set; }

    public string? Bio { get; set; }

    public string? AvatarUrl { get; set; }

    public bool PrivacyAccepted { get; set; }

    public DateTimeOffset? PrivacyAcceptedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? LastAccessedAtUtc { get; set; }

    public FavoriteListRecord? FavoriteList { get; set; }

    public ICollection<PlaceReviewRecord> Reviews { get; set; } = [];

    public ICollection<PrivacyConsentEventRecord> PrivacyConsentEvents { get; set; } = [];
}
