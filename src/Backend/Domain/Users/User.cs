using YepPet.Domain.Common;
using YepPet.Domain.Users.ValueObjects;

namespace YepPet.Domain.Users;

public sealed class User : AggregateRoot<Guid>
{
    public User(
        Guid id,
        string email,
        string passwordHash,
        string role,
        UserProfile profile,
        PrivacyConsent privacyConsent,
        DateTimeOffset? createdAtUtc = null,
        DateTimeOffset? lastAccessedAtUtc = null) : base(id)
    {
        SetEmail(email);
        SetPasswordHash(passwordHash);
        Role = NormalizeRole(role);
        Profile = profile;
        PrivacyConsent = privacyConsent;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
        LastAccessedAtUtc = lastAccessedAtUtc;
    }

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Role key matching <c>roles.key</c> (e.g. Admin, User, custom roles).</summary>
    public string Role { get; private set; } = string.Empty;

    public UserProfile Profile { get; private set; }

    public PrivacyConsent PrivacyConsent { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? LastAccessedAtUtc { get; private set; }

    public void UpdateProfile(UserProfile profile)
    {
        if (IsStandardUserRole() && !PrivacyConsent.Accepted)
        {
            throw new DomainRuleException("User profile cannot be updated without privacy consent.");
        }

        Profile = profile;
    }

    public void AcceptPrivacy(DateTimeOffset acceptedAtUtc)
    {
        PrivacyConsent = new PrivacyConsent(true, acceptedAtUtc);
    }

    public void RevokePrivacy()
    {
        if (IsStandardUserRole())
        {
            throw new DomainRuleException("User privacy consent cannot be revoked while profile remains active.");
        }

        PrivacyConsent = new PrivacyConsent(false, null);
    }

    public void ChangeRole(string role)
    {
        Role = NormalizeRole(role);
    }

    public void RecordAccess(DateTimeOffset accessedAtUtc)
    {
        LastAccessedAtUtc = accessedAtUtc;
    }

    /// <summary>Standard app users subject to privacy gating (role key "User", any casing).</summary>
    private bool IsStandardUserRole()
    {
        return string.Equals(Role, "User", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new DomainRuleException("Role is required.");
        }

        return role.Trim();
    }

    private void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new DomainRuleException("A valid email is required.");
        }

        Email = email.Trim().ToLowerInvariant();
    }

    private void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainRuleException("Password hash is required.");
        }

        PasswordHash = passwordHash.Trim();
    }
}
