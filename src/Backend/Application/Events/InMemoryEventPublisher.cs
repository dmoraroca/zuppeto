using Microsoft.Extensions.DependencyInjection;

namespace YepPet.Application.Events;

public sealed class InMemoryEventPublisher(IServiceProvider services) : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var handlers = services.GetServices<IEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(evt, cancellationToken);
        }
    }
}
