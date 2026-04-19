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
    IRoleCatalogRepository roleCatalogRepository,
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
        var roleKey = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role.Trim();
        var catalogEntry = await roleCatalogRepository.GetByKeyAsync(roleKey, cancellationToken);
        if (catalogEntry is null || !catalogEntry.IsActive)
        {
            return Result<UserDto>.Fail(FailureKind.Conflict, "Rol invàlid o inactiu.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var displayName = request.DisplayName.Trim();
        var city = request.City.Trim();
        var country = request.Country.Trim();
        var avatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            return Result<UserDto>.Fail(FailureKind.Conflict, $"User '{email}' already exists.");
        }

        var user = new User(
            Guid.NewGuid(),
            email,
            passwordHasher.Hash(request.Password.Trim()),
            catalogEntry.Key,
            userProfileFactory.Create(displayName, city, country, string.Empty, avatarUrl),
            new Domain.Users.ValueObjects.PrivacyConsent(false, null));

        await userRepository.AddAsync(user, cancellationToken);
        await eventPublisher.PublishAsync(
            new UserCreatedEvent(user.Id, user.Email, user.Role),
            cancellationToken);

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.Email,
            user.Role,
            user.Profile.DisplayName,
            user.Profile.City,
            user.Profile.Country,
            user.Profile.Bio,
            user.Profile.AvatarUrl,
            user.PrivacyConsent.Accepted,
            user.PrivacyConsent.AcceptedAtUtc));
    }
}
