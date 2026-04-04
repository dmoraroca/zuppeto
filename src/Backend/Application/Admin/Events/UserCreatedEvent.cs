using YepPet.Application.Events;

namespace YepPet.Application.Admin.Events;

public sealed record UserCreatedEvent(Guid UserId, string Email, string Role) : IDomainEvent
{
    public DateTimeOffset OccurredAtUtc { get; } = DateTimeOffset.UtcNow;
}
