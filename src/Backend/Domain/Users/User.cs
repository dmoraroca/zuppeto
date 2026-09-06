using Zuppeto.Domain.Common;
using Zuppeto.Domain.Users.ValueObjects;

namespace Zuppeto.Domain.Users;

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
            throw new DomainRuleException("No es pot actualitzar el perfil sense consentiment de privacitat.");
        }

        ReplaceProfile(profile);
    }

    /// <summary>Admin maintenance: replace profile fields without the self-service privacy gate.</summary>
    public void ReplaceProfile(UserProfile profile)
    {
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
            throw new DomainRuleException("No es pot revocar el consentiment mentre el perfil estigui actiu.");
        }

        PrivacyConsent = new PrivacyConsent(false, null);
    }

    public void ChangeRole(string role)
    {
        Role = NormalizeRole(role);
    }

    public void ChangeEmail(string email)
    {
        SetEmail(email);
    }

    public void ChangePasswordHash(string passwordHash)
    {
        SetPasswordHash(passwordHash);
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
            throw new DomainRuleException("El rol és obligatori.");
        }

        return role.Trim();
    }

    private void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new DomainRuleException("Cal un email vàlid.");
        }

        Email = email.Trim().ToLowerInvariant();
    }

    private void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainRuleException("El hash de la contrasenya és obligatori.");
        }

        PasswordHash = passwordHash.Trim();
    }
}
