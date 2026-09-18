using Zuppeto.Domain.Common;
using Zuppeto.Domain.Users.ValueObjects;

namespace Zuppeto.Domain.Users;

public sealed class User : AggregateRoot<Guid>
{
    public User(
        Guid id,
        string email,
        string? passwordHash,
        string role,
        UserProfile profile,
        PrivacyConsent privacyConsent,
        DateTimeOffset? createdAtUtc = null,
        DateTimeOffset? lastAccessedAtUtc = null,
        DateTimeOffset? emailActivatedAtUtc = null,
        string? activationTokenHash = null,
        DateTimeOffset? activationTokenExpiresAtUtc = null,
        DateTimeOffset? activationTokenUsedAtUtc = null,
        bool emailActivationManaged = false,
        string? passwordResetTokenHash = null,
        DateTimeOffset? passwordResetTokenExpiresAtUtc = null,
        DateTimeOffset? passwordResetTokenUsedAtUtc = null,
        int securityVersion = 1,
        string? totpSecretProtected = null,
        string? pendingTotpSecretProtected = null,
        DateTimeOffset? pendingTotpExpiresAtUtc = null,
        DateTimeOffset? totpEnabledAtUtc = null,
        long? lastTotpTimeStepUsed = null) : base(id)
    {
        SetEmail(email);
        if (!string.IsNullOrWhiteSpace(passwordHash)) SetPasswordHash(passwordHash);
        Role = NormalizeRole(role);
        Profile = profile;
        PrivacyConsent = privacyConsent;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
        LastAccessedAtUtc = lastAccessedAtUtc;
        EmailActivatedAtUtc = emailActivationManaged ? emailActivatedAtUtc : emailActivatedAtUtc ?? CreatedAtUtc;
        ActivationTokenHash = activationTokenHash;
        ActivationTokenExpiresAtUtc = activationTokenExpiresAtUtc;
        ActivationTokenUsedAtUtc = activationTokenUsedAtUtc;
        PasswordResetTokenHash = passwordResetTokenHash;
        PasswordResetTokenExpiresAtUtc = passwordResetTokenExpiresAtUtc;
        PasswordResetTokenUsedAtUtc = passwordResetTokenUsedAtUtc;
        SecurityVersion = Math.Max(1, securityVersion);
        TotpSecretProtected = totpSecretProtected;
        PendingTotpSecretProtected = pendingTotpSecretProtected;
        PendingTotpExpiresAtUtc = pendingTotpExpiresAtUtc;
        TotpEnabledAtUtc = totpEnabledAtUtc;
        LastTotpTimeStepUsed = lastTotpTimeStepUsed;
    }

    public string Email { get; private set; } = string.Empty;

    public string? PasswordHash { get; private set; }

    public bool HasLocalCredential => !string.IsNullOrWhiteSpace(PasswordHash);

    /// <summary>Role key matching <c>roles.key</c> (e.g. Admin, User, custom roles).</summary>
    public string Role { get; private set; } = string.Empty;

    public UserProfile Profile { get; private set; }

    public PrivacyConsent PrivacyConsent { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? LastAccessedAtUtc { get; private set; }

    public DateTimeOffset? EmailActivatedAtUtc { get; private set; }

    public string? ActivationTokenHash { get; private set; }

    public DateTimeOffset? ActivationTokenExpiresAtUtc { get; private set; }

    public DateTimeOffset? ActivationTokenUsedAtUtc { get; private set; }

    public string? PasswordResetTokenHash { get; private set; }

    public DateTimeOffset? PasswordResetTokenExpiresAtUtc { get; private set; }

    public DateTimeOffset? PasswordResetTokenUsedAtUtc { get; private set; }

    public int SecurityVersion { get; private set; }

    public string? TotpSecretProtected { get; private set; }
    public string? PendingTotpSecretProtected { get; private set; }
    public DateTimeOffset? PendingTotpExpiresAtUtc { get; private set; }
    public DateTimeOffset? TotpEnabledAtUtc { get; private set; }
    public long? LastTotpTimeStepUsed { get; private set; }
    public bool IsTotpEnabled => !string.IsNullOrWhiteSpace(TotpSecretProtected) && TotpEnabledAtUtc is not null;

    public void StartTotpSetup(string protectedSecret, DateTimeOffset expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(protectedSecret) || expiresAtUtc <= DateTimeOffset.UtcNow) throw new DomainRuleException("La configuració TOTP no és vàlida.");
        PendingTotpSecretProtected = protectedSecret;
        PendingTotpExpiresAtUtc = expiresAtUtc;
    }

    public void ConfirmTotpSetup(DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(PendingTotpSecretProtected) || PendingTotpExpiresAtUtc is null || PendingTotpExpiresAtUtc <= nowUtc)
            throw new DomainRuleException("La configuració TOTP ha caducat.");
        TotpSecretProtected = PendingTotpSecretProtected;
        TotpEnabledAtUtc = nowUtc;
        LastTotpTimeStepUsed = null;
        PendingTotpSecretProtected = null;
        PendingTotpExpiresAtUtc = null;
        SecurityVersion++;
    }

    public void DisableTotp()
    {
        TotpSecretProtected = null;
        PendingTotpSecretProtected = null;
        PendingTotpExpiresAtUtc = null;
        TotpEnabledAtUtc = null;
        LastTotpTimeStepUsed = null;
        SecurityVersion++;
    }

    public bool TryUseTotpTimeStep(long timeStep)
    {
        if (!IsTotpEnabled || timeStep < 0 || (LastTotpTimeStepUsed is not null && timeStep <= LastTotpTimeStepUsed)) return false;
        LastTotpTimeStepUsed = timeStep;
        return true;
    }

    public bool IsEmailActivated => EmailActivatedAtUtc is not null;

    public void RequireEmailActivation(string tokenHash, DateTimeOffset expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new DomainRuleException("El token d'activació és obligatori.");
        if (expiresAtUtc <= DateTimeOffset.UtcNow) throw new DomainRuleException("El token d'activació ha de caducar en el futur.");

        EmailActivatedAtUtc = null;
        ActivationTokenHash = tokenHash;
        ActivationTokenExpiresAtUtc = expiresAtUtc;
        ActivationTokenUsedAtUtc = null;
    }

    public ActivationTokenValidationResult ActivateEmail(string tokenHash, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(ActivationTokenHash) || !string.Equals(ActivationTokenHash, tokenHash, StringComparison.Ordinal))
            return ActivationTokenValidationResult.Invalid;
        if (ActivationTokenUsedAtUtc is not null || IsEmailActivated)
            return ActivationTokenValidationResult.Used;
        if (ActivationTokenExpiresAtUtc is null || ActivationTokenExpiresAtUtc <= nowUtc)
            return ActivationTokenValidationResult.Expired;

        EmailActivatedAtUtc = nowUtc;
        ActivationTokenUsedAtUtc = nowUtc;
        return ActivationTokenValidationResult.Activated;
    }

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

    public void StartPasswordReset(string tokenHash, DateTimeOffset expiresAtUtc)
    {
        if (!HasLocalCredential) throw new DomainRuleException("Aquest compte no té credencial local.");
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new DomainRuleException("El token de recuperació és obligatori.");
        if (expiresAtUtc <= DateTimeOffset.UtcNow) throw new DomainRuleException("El token de recuperació ha de caducar en el futur.");

        PasswordResetTokenHash = tokenHash;
        PasswordResetTokenExpiresAtUtc = expiresAtUtc;
        PasswordResetTokenUsedAtUtc = null;
    }

    public PasswordResetTokenValidationResult ResetPassword(string tokenHash, string passwordHash, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(PasswordResetTokenHash) || !string.Equals(PasswordResetTokenHash, tokenHash, StringComparison.Ordinal))
            return PasswordResetTokenValidationResult.Invalid;
        if (PasswordResetTokenUsedAtUtc is not null)
            return PasswordResetTokenValidationResult.Used;
        if (PasswordResetTokenExpiresAtUtc is null || PasswordResetTokenExpiresAtUtc <= nowUtc)
            return PasswordResetTokenValidationResult.Expired;

        SetPasswordHash(passwordHash);
        PasswordResetTokenUsedAtUtc = nowUtc;
        SecurityVersion++;
        return PasswordResetTokenValidationResult.Reset;
    }

    public void RecordAccess(DateTimeOffset accessedAtUtc)
    {
        LastAccessedAtUtc = accessedAtUtc;
    }

    public void RevokeSessions()
    {
        SecurityVersion++;
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

public enum ActivationTokenValidationResult
{
    Activated,
    Invalid,
    Expired,
    Used
}

public enum PasswordResetTokenValidationResult
{
    Reset,
    Invalid,
    Expired,
    Used
}
