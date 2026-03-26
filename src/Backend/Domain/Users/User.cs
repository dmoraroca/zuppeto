using YepPet.Domain.Common;
using YepPet.Domain.Users.ValueObjects;

namespace YepPet.Domain.Users;

public sealed class User : AggregateRoot<Guid>
{
    public User(
        Guid id,
        string email,
        string passwordHash,
        UserRole role,
        UserProfile profile,
        PrivacyConsent privacyConsent) : base(id)
    {
        SetEmail(email);
        SetPasswordHash(passwordHash);
        Role = role;
        Profile = profile;
        PrivacyConsent = privacyConsent;
    }

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public UserProfile Profile { get; private set; }

    public PrivacyConsent PrivacyConsent { get; private set; }

    public void UpdateProfile(UserProfile profile)
    {
        if (Role == UserRole.User && !PrivacyConsent.Accepted)
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
        if (Role == UserRole.User)
        {
            throw new DomainRuleException("User privacy consent cannot be revoked while profile remains active.");
        }

        PrivacyConsent = new PrivacyConsent(false, null);
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
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
