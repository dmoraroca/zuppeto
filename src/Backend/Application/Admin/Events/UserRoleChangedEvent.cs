using YepPet.Application.Events;

namespace YepPet.Application.Admin.Events;

public sealed record UserRoleChangedEvent(Guid UserId, string OldRole, string NewRole) : IDomainEvent
{
    public DateTimeOffset OccurredAtUtc { get; } = DateTimeOffset.UtcNow;
}
