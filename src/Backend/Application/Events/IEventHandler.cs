namespace YepPet.Application.Events;

public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent evt, CancellationToken cancellationToken = default);
}
