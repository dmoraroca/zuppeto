using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Users;
using Zuppeto.Domain.Users.ValueObjects;
using Zuppeto.Application.Factories;
using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Users;

internal sealed class UserApplicationService(
    IUserRepository userRepository,
    Auth.IPasswordHasher passwordHasher,
    IUserProfileFactory userProfileFactory) : IUserApplicationService
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
        var user = new User(
            Guid.NewGuid(),
            request.Email,
            passwordHasher.Hash(request.PasswordHash),
            request.Role.Trim(),
            userProfileFactory.Create(request.DisplayName, request.City, request.Country, request.Bio, request.AvatarUrl),
            new PrivacyConsent(request.PrivacyAccepted, request.PrivacyAcceptedAtUtc));

        await userRepository.AddAsync(user, cancellationToken);
        return user.Id;
    }

    public async Task UpdateProfileAsync(UserProfileUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"User '{request.Id}' was not found.");

        if (request.PrivacyAccepted && !user.PrivacyConsent.Accepted)
        {
            user.AcceptPrivacy(request.PrivacyAcceptedAtUtc ?? DateTimeOffset.UtcNow);
        }

        user.UpdateProfile(
            userProfileFactory.Create(
                request.DisplayName,
                request.City,
                request.Country,
                request.Bio,
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
            if (!passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
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
        return user is null ? null : new UserPasswordVerifyDto(passwordHasher.Verify(user.PasswordHash, password));
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
            user.Profile.Bio,
            user.Profile.AvatarUrl,
            user.PrivacyConsent.Accepted,
            user.PrivacyConsent.AcceptedAtUtc);
    }
}
