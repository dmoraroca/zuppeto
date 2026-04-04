using YepPet.Application.Commands;
using YepPet.Application.Admin.Events;
using YepPet.Application.Events;
using YepPet.Application.Factories;
using YepPet.Application.Results;
using YepPet.Application.Users;
using YepPet.Domain.Abstractions;
using YepPet.Domain.Users;

namespace YepPet.Application.Admin.Commands;

public sealed class CreateAdminUserCommandHandler(
    IUserRepository userRepository,
    Auth.IPasswordHasher passwordHasher,
    IUserProfileFactory userProfileFactory,
    IEventPublisher eventPublisher)
    : ICommandHandler<CreateAdminUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> HandleAsync(
        CreateAdminUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;
        var role = Enum.Parse<UserRole>(request.Role, ignoreCase: true);
        var email = request.Email.Trim().ToLowerInvariant();
        var displayName = request.DisplayName.Trim();

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            return Result<UserDto>.Fail(FailureKind.Conflict, $"User '{email}' already exists.");
        }

        var user = new Domain.Users.User(
            Guid.NewGuid(),
            email,
            passwordHasher.Hash(request.Password.Trim()),
            role,
            userProfileFactory.Create(displayName, string.Empty, string.Empty, string.Empty, null),
            new Domain.Users.ValueObjects.PrivacyConsent(false, null));

            await userRepository.AddAsync(user, cancellationToken);
            await eventPublisher.PublishAsync(
                new UserCreatedEvent(user.Id, user.Email, user.Role.ToString()),
            cancellationToken);

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.Profile.DisplayName,
            user.Profile.City,
            user.Profile.Country,
            user.Profile.Bio,
            user.Profile.AvatarUrl,
            user.PrivacyConsent.Accepted,
            user.PrivacyConsent.AcceptedAtUtc));
    }
}
