namespace YepPet.Application.Events;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}
