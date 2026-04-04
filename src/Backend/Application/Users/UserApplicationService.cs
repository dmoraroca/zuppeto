using YepPet.Domain.Abstractions;
using YepPet.Domain.Users;
using YepPet.Domain.Users.ValueObjects;
using YepPet.Application.Factories;

namespace YepPet.Application.Users;

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
            Enum.Parse<UserRole>(request.Role, ignoreCase: true),
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

    private static UserDto ToDto(User user)
    {
        return new UserDto(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.Profile.DisplayName,
            user.Profile.City,
            user.Profile.Country,
            user.Profile.Bio,
            user.Profile.AvatarUrl,
            user.PrivacyConsent.Accepted,
            user.PrivacyConsent.AcceptedAtUtc);
    }
}
