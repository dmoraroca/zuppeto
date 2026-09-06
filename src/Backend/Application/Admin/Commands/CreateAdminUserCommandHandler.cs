using Zuppeto.Application.Commands;
using Zuppeto.Application.Admin.Events;
using Zuppeto.Application.Events;
using Zuppeto.Application.Factories;
using Zuppeto.Application.Results;
using Zuppeto.Application.Users;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Users;

namespace Zuppeto.Application.Admin.Commands;

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
            return Result<UserDto>.Fail(FailureKind.Conflict, $"Ja existeix un usuari amb l’email «{email}».");
        }

        var user = new User(
            Guid.NewGuid(),
            email,
            // Login password: hash of Password after ConfirmPassword matched (same column as profile).
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
            user.Profile.Comments,
            user.Profile.AvatarUrl,
            user.PrivacyConsent.Accepted,
            user.PrivacyConsent.AcceptedAtUtc));
    }
}
