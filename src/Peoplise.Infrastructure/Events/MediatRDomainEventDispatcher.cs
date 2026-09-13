using MediatR;
using Peoplise.SharedKernel.Domain;
using Peoplise.SharedKernel.Events;

namespace Peoplise.Infrastructure.Events;

/// <summary>
/// Publishes domain events in-process via MediatR — the "modules communicate through
/// MediatR notifications" decision from Stage 1's architecture summary. Each event is
/// wrapped as a <see cref="DomainEventNotification{TDomainEvent}"/> so it can go through
/// MediatR's typed <see cref="INotificationHandler{TNotification}"/> dispatch; handlers
/// run in the same process and the same request, not on a queue.
/// </summary>
public sealed class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;

    public MediatRDomainEventDispatcher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

            await _publisher.Publish(notification, cancellationToken);
        }
    }
}
