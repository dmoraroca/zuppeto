namespace YepPet.Application.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredAtUtc { get; }
}
