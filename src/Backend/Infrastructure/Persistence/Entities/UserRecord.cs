namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class UserRecord
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public string Role { get; set; } = string.Empty;

    public RoleRecord? RoleRef { get; set; }

    public string? DisplayName { get; set; }

    public string? City { get; set; }

    public string? Country { get; set; }

    public Guid? TerritorialCountryId { get; set; }

    public CountryRecord? TerritorialCountry { get; set; }

    public Guid? TerritorialUnitId { get; set; }

    public TerritorialUnitRecord? TerritorialUnit { get; set; }

    public string? Comments { get; set; }

    public string? AvatarUrl { get; set; }

    public bool PrivacyAccepted { get; set; }

    public DateTimeOffset? PrivacyAcceptedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? LastAccessedAtUtc { get; set; }

    public DateTimeOffset? EmailActivatedAtUtc { get; set; }

    public string? ActivationTokenHash { get; set; }

    public DateTimeOffset? ActivationTokenExpiresAtUtc { get; set; }

    public DateTimeOffset? ActivationTokenUsedAtUtc { get; set; }

    public string? PasswordResetTokenHash { get; set; }

    public DateTimeOffset? PasswordResetTokenExpiresAtUtc { get; set; }

    public DateTimeOffset? PasswordResetTokenUsedAtUtc { get; set; }

    public int SecurityVersion { get; set; } = 1;

    public string? TotpSecretProtected { get; set; }
    public string? PendingTotpSecretProtected { get; set; }
    public DateTimeOffset? PendingTotpExpiresAtUtc { get; set; }
    public DateTimeOffset? TotpEnabledAtUtc { get; set; }
    public long? LastTotpTimeStepUsed { get; set; }

    public ICollection<ExternalIdentityRecord> ExternalIdentities { get; set; } = [];

    public FavoriteListRecord? FavoriteList { get; set; }

    public ICollection<PlaceReviewRecord> Reviews { get; set; } = [];

    public ICollection<PrivacyConsentEventRecord> PrivacyConsentEvents { get; set; } = [];
}
