using Microsoft.Extensions.Logging;
using YepPet.Application.Events;

namespace YepPet.Application.Admin.Events;

public sealed class AuditUserEventsHandler(ILogger<AuditUserEventsHandler> logger)
    : IEventHandler<UserCreatedEvent>, IEventHandler<UserRoleChangedEvent>
{
    public Task HandleAsync(UserCreatedEvent evt, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "User created. Id={UserId} Email={Email} Role={Role}",
            evt.UserId,
            evt.Email,
            evt.Role);
        return Task.CompletedTask;
    }

    public Task HandleAsync(UserRoleChangedEvent evt, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "User role changed. Id={UserId} {OldRole} -> {NewRole}",
            evt.UserId,
            evt.OldRole,
            evt.NewRole);
        return Task.CompletedTask;
    }
}
