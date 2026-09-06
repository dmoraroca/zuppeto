using Zuppeto.Application.Commands;
using Zuppeto.Application.Admin.Events;
using Zuppeto.Application.Events;
using Zuppeto.Application.Results;
using Zuppeto.Application.Users;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Users;

namespace Zuppeto.Application.Admin.Commands;

public sealed class UpdateUserRoleCommandHandler(
    IUserRepository userRepository,
    IRoleCatalogRepository roleCatalogRepository,
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
            return Result<UserDto>.Fail(FailureKind.NotFound, "No s’ha trobat l’usuari.");
        }

        var catalogEntry = await roleCatalogRepository.GetByKeyAsync(command.Request.Role.Trim(), cancellationToken);
        if (catalogEntry is null || !catalogEntry.IsActive)
        {
            return Result<UserDto>.Fail(FailureKind.Conflict, "Rol invàlid o inactiu.");
        }

        var previousRole = user.Role;
        user.ChangeRole(catalogEntry.Key);
        await userRepository.UpdateAsync(user, cancellationToken);

        await eventPublisher.PublishAsync(
            new UserRoleChangedEvent(user.Id, previousRole, user.Role),
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
