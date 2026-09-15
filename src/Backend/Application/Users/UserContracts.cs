namespace Zuppeto.Application.Users;

public sealed record UserDto(
    Guid Id,
    string Email,
    string Role,
    string DisplayName,
    string City,
    string Country,
    string Comments,
    string? AvatarUrl,
    bool PrivacyAccepted,
    DateTimeOffset? PrivacyAcceptedAtUtc,
    bool HasLocalCredential = true,
    bool IsTotpEnabled = false);

public sealed record UserRegistrationRequest(
    string Email,
    string PasswordHash,
    string Role,
    string DisplayName,
    string City,
    string Country,
    string Comments,
    string? AvatarUrl,
    bool PrivacyAccepted,
    DateTimeOffset? PrivacyAcceptedAtUtc);

public sealed record AccountActivationRequest(string Token);

public sealed record ActivationEmailResendRequest(string Email);

public enum AccountActivationStatus { Activated, Invalid, Expired, Used }

public sealed record AccountActivationResult(string Status);

public sealed record PasswordRecoveryRequest(string Email);

public sealed record PasswordResetRequest(string Token, string NewPassword, string ConfirmNewPassword);

public enum PasswordResetStatus { Reset, Invalid, Expired, Used }

public sealed record PasswordResetResult(string Status);
public sealed record TotpSetupDto(string QrSvg, string ManualEntryKey, DateTimeOffset ExpiresAtUtc);
public sealed record TotpRecoveryCodesDto(IReadOnlyCollection<string> Codes);

public sealed record UserProfileUpdateRequest(
    Guid Id,
    string DisplayName,
    string City,
    string Country,
    string Comments,
    string? AvatarUrl,
    bool PrivacyAccepted,
    DateTimeOffset? PrivacyAcceptedAtUtc);

public sealed record UserAccountUpdateRequest(
    Guid Id,
    string Email,
    string CurrentPassword,
    string? NewPassword,
    string? ConfirmNewPassword);

public sealed record UserPasswordVerifyRequest(string Password);

public sealed record UserPasswordVerifyDto(bool Matches);
