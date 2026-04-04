using YepPet.Application.Commands;
using YepPet.Application.Admin.Events;
using YepPet.Application.Events;
using YepPet.Application.Results;
using YepPet.Application.Users;
using YepPet.Domain.Abstractions;
using YepPet.Domain.Users;

namespace YepPet.Application.Admin.Commands;

public sealed class UpdateUserRoleCommandHandler(
    IUserRepository userRepository,
    IEventPublisher eventPublisher)
    : ICommandHandler<UpdateUserRoleCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> HandleAsync(
        UpdateUserRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result<UserDto>.Fail(FailureKind.NotFound, $"User '{command.UserId}' was not found.");
        }

        var nextRole = Enum.Parse<UserRole>(command.Request.Role, ignoreCase: true);
        var previousRole = user.Role;
        user.ChangeRole(nextRole);
        await userRepository.UpdateAsync(user, cancellationToken);

        await eventPublisher.PublishAsync(
            new UserRoleChangedEvent(user.Id, previousRole.ToString(), nextRole.ToString()),
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
