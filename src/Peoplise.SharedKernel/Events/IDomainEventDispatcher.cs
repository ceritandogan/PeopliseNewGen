using Peoplise.SharedKernel.Domain;

namespace Peoplise.SharedKernel.Events;

/// <summary>
/// Publishes the domain events raised by an aggregate to whatever in-process handlers
/// are registered for them. The <c>Peoplise.Infrastructure</c> layer provides the
/// concrete implementation (backed by MediatR's <c>INotification</c> publishing);
/// <c>Peoplise.SharedKernel</c> only depends on this abstraction, so the domain model
/// stays free of any messaging-library reference.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Publishes each event in <paramref name="domainEvents"/> in turn. Called by the
    /// unit of work after <c>SaveChanges</c> has committed, so handlers only ever see
    /// events for state that is already durable.
    /// </summary>
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
