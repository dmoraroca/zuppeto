using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Users;
using Zuppeto.Domain.Users.ValueObjects;
using Zuppeto.Application.Factories;
using Zuppeto.Application.Validation;
using System.Security.Cryptography;
using System.Text;

namespace Zuppeto.Application.Users;

internal sealed class UserApplicationService(
    IUserRepository userRepository,
    Auth.IPasswordHasher passwordHasher,
    IUserProfileFactory userProfileFactory,
    IAccountActivationEmailSender activationEmailSender,
    IPasswordRecoveryEmailSender passwordRecoveryEmailSender,
    Auth.ITotpService totpService,
    ITotpRecoveryCodeRepository totpRecoveryCodes,
    Auth.ITwoFactorChallengeStore challenges) : IUserApplicationService
{
    public async Task<IReadOnlyCollection<UserDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.ListAsync(cancellationToken);
        return users.Select(ToDto).ToArray();
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : ToDto(user);
    }

    public async Task<UserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        return user is null ? null : ToDto(user);
    }

    public async Task<Guid> RegisterAsync(UserRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var rawToken = CreateRawToken();
        var expiresAtUtc = DateTimeOffset.UtcNow.AddHours(24);
        var user = new User(
            Guid.NewGuid(),
            request.Email,
            passwordHasher.Hash(request.PasswordHash),
            request.Role.Trim(),
            userProfileFactory.Create(request.DisplayName, request.City, request.Country, request.Comments, request.AvatarUrl),
            new PrivacyConsent(request.PrivacyAccepted, request.PrivacyAcceptedAtUtc));
        user.RequireEmailActivation(HashToken(rawToken), expiresAtUtc);

        await userRepository.AddAsync(user, cancellationToken);
        await activationEmailSender.SendAsync(new AccountActivationEmail(user.Email, rawToken, expiresAtUtc), cancellationToken);
        return user.Id;
    }

    public async Task<AccountActivationResult> ActivateEmailAsync(
        AccountActivationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token)) return new(AccountActivationStatus.Invalid.ToString());
        var tokenHash = HashToken(request.Token);
        var user = await userRepository.GetByActivationTokenHashAsync(tokenHash, cancellationToken);
        if (user is null) return new(AccountActivationStatus.Invalid.ToString());

        var result = user.ActivateEmail(tokenHash, DateTimeOffset.UtcNow);
        if (result == ActivationTokenValidationResult.Activated) await userRepository.UpdateAsync(user, cancellationToken);
        return new((result switch
        {
            ActivationTokenValidationResult.Activated => AccountActivationStatus.Activated,
            ActivationTokenValidationResult.Expired => AccountActivationStatus.Expired,
            ActivationTokenValidationResult.Used => AccountActivationStatus.Used,
            _ => AccountActivationStatus.Invalid
        }).ToString());
    }

    public async Task ResendActivationEmailAsync(ActivationEmailResendRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) return;
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.IsEmailActivated) return;

        var rawToken = CreateRawToken();
        var expiresAtUtc = DateTimeOffset.UtcNow.AddHours(24);
        user.RequireEmailActivation(HashToken(rawToken), expiresAtUtc);
        await userRepository.UpdateAsync(user, cancellationToken);
        await activationEmailSender.SendAsync(new AccountActivationEmail(user.Email, rawToken, expiresAtUtc), cancellationToken);
    }

    public async Task RequestPasswordRecoveryAsync(PasswordRecoveryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) return;
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !user.HasLocalCredential) return;

        var rawToken = CreateRawToken();
        var expiresAtUtc = DateTimeOffset.UtcNow.AddHours(1);
        user.StartPasswordReset(HashToken(rawToken), expiresAtUtc);
        await userRepository.UpdateAsync(user, cancellationToken);
        await passwordRecoveryEmailSender.SendAsync(new PasswordRecoveryEmail(user.Email, rawToken, expiresAtUtc), cancellationToken);
    }

    public async Task<PasswordResetResult> ResetPasswordAsync(PasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token)) return new(PasswordResetStatus.Invalid.ToString());
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Trim().Length < 6 ||
            !string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
            return new(PasswordResetStatus.Invalid.ToString());

        var tokenHash = HashToken(request.Token);
        var user = await userRepository.GetByPasswordResetTokenHashAsync(tokenHash, cancellationToken);
        if (user is null) return new(PasswordResetStatus.Invalid.ToString());
        var result = user.ResetPassword(tokenHash, passwordHasher.Hash(request.NewPassword.Trim()), DateTimeOffset.UtcNow);
        if (result == PasswordResetTokenValidationResult.Reset) await userRepository.UpdateAsync(user, cancellationToken);
        return new((result switch
        {
            PasswordResetTokenValidationResult.Reset => PasswordResetStatus.Reset,
            PasswordResetTokenValidationResult.Expired => PasswordResetStatus.Expired,
            PasswordResetTokenValidationResult.Used => PasswordResetStatus.Used,
            _ => PasswordResetStatus.Invalid
        }).ToString());
    }

    public async Task<TotpSetupDto> StartTotpSetupAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken) ?? throw new InvalidOperationException("No s’ha trobat l’usuari.");
        var material = totpService.CreateSetup(user.Email);
        var expires = DateTimeOffset.UtcNow.AddMinutes(10);
        user.StartTotpSetup(material.ProtectedSecret, expires);
        await userRepository.UpdateAsync(user, cancellationToken);
        return new(material.QrSvg, material.ManualEntryKey, expires);
    }

    public async Task<TotpRecoveryCodesDto?> ConfirmTotpSetupAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user?.PendingTotpSecretProtected is null || !totpService.Verify(user.PendingTotpSecretProtected, code, DateTimeOffset.UtcNow).IsValid) return null;
        user.ConfirmTotpSetup(DateTimeOffset.UtcNow);
        var codes = totpService.CreateRecoveryCodes();
        await totpRecoveryCodes.ReplaceAsync(userId, codes.Select(totpService.HashRecoveryCode).ToArray(), cancellationToken);
        await userRepository.UpdateAsync(user, cancellationToken);
        return new(codes);
    }

    public async Task<bool> DisableTotpAsync(Guid userId, string verificationCode, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsTotpEnabled || user.TotpSecretProtected is null) return false;
        var verified = totpService.Verify(user.TotpSecretProtected, verificationCode, DateTimeOffset.UtcNow).IsValid ||
            await totpRecoveryCodes.ConsumeAsync(userId, totpService.HashRecoveryCode(verificationCode), cancellationToken);
        if (!verified) return false;
        user.DisableTotp();
        challenges.RevokeForUser(userId);
        await totpRecoveryCodes.DeleteAsync(userId, cancellationToken);
        await userRepository.UpdateAsync(user, cancellationToken);
        return true;
    }

    public async Task<TotpRecoveryCodesDto?> RegenerateTotpRecoveryCodesAsync(Guid userId, string verificationCode, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user?.TotpSecretProtected is null || !user.IsTotpEnabled || !totpService.Verify(user.TotpSecretProtected, verificationCode, DateTimeOffset.UtcNow).IsValid) return null;
        var codes = totpService.CreateRecoveryCodes();
        await totpRecoveryCodes.ReplaceAsync(userId, codes.Select(totpService.HashRecoveryCode).ToArray(), cancellationToken);
        return new(codes);
    }

    public async Task UpdateProfileAsync(UserProfileUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("No s’ha trobat l’usuari.");

        if (request.PrivacyAccepted && !user.PrivacyConsent.Accepted)
        {
            user.AcceptPrivacy(request.PrivacyAcceptedAtUtc ?? DateTimeOffset.UtcNow);
        }

        user.UpdateProfile(
            userProfileFactory.Create(
                request.DisplayName,
                request.City,
                request.Country,
                request.Comments,
                request.AvatarUrl));

        await userRepository.UpdateAsync(user, cancellationToken);
    }

    public async Task<ValidationResult> ChangeAccountAsync(
        UserAccountUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = ValidationResult.Success();
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user is null)
        {
            result.Add(nameof(request.Id), "No s’ha trobat l’usuari.");
            return result;
        }

        var nextEmail = request.Email.Trim().ToLowerInvariant();
        var emailChanged = !string.Equals(user.Email, nextEmail, StringComparison.Ordinal);
        if (emailChanged)
        {
            var existing = await userRepository.GetByEmailAsync(nextEmail, cancellationToken);
            if (existing is not null && existing.Id != user.Id)
            {
                result.Add(nameof(request.Email), "Aquest email ja està en ús.");
                return result;
            }

            user.ChangeEmail(nextEmail);
        }

        var newPassword = request.NewPassword?.Trim() ?? string.Empty;
        if (newPassword.Length > 0)
        {
            if (!user.HasLocalCredential || !passwordHasher.Verify(user.PasswordHash!, request.CurrentPassword))
            {
                result.Add(nameof(request.CurrentPassword), "La contrasenya actual no és correcta.");
                return result;
            }

            user.ChangePasswordHash(passwordHasher.Hash(newPassword));
        }

        if (emailChanged || newPassword.Length > 0)
        {
            await userRepository.UpdateAsync(user, cancellationToken);
        }

        return result;
    }

    public async Task<UserPasswordVerifyDto?> VerifyCurrentPasswordAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null || !user.HasLocalCredential ? null : new UserPasswordVerifyDto(passwordHasher.Verify(user.PasswordHash!, password));
    }

    private static UserDto ToDto(User user)
    {
        return new UserDto(
            user.Id,
            user.Email,
            user.Role,
            user.Profile.DisplayName,
            user.Profile.City,
            user.Profile.Country,
            user.Profile.Comments,
            user.Profile.AvatarUrl,
            user.PrivacyConsent.Accepted,
            user.PrivacyConsent.AcceptedAtUtc,
            user.HasLocalCredential,
            user.IsTotpEnabled);
    }

    private static string CreateRawToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
