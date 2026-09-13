using MediatR;
using Peoplise.SharedKernel.Domain;

namespace Peoplise.Infrastructure.Events;

/// <summary>
/// Wraps a <see cref="IDomainEvent"/> as a MediatR <see cref="INotification"/>. Domain
/// events themselves stay free of any MediatR reference (<c>Peoplise.SharedKernel</c>
/// doesn't depend on MediatR at all) — this wrapper is what lets
/// <see cref="MediatRDomainEventDispatcher"/> publish an arbitrary domain event through
/// MediatR's typed notification pipeline. A module handles a specific event by
/// implementing <c>INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;</c>.
/// </summary>
public sealed class DomainEventNotification<TDomainEvent> : INotification
    where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; }

    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}
