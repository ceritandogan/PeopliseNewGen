namespace Peoplise.SharedKernel.Domain;

/// <summary>
/// Marker interface for a domain event: something that happened inside an aggregate
/// that other parts of the system may need to react to.
/// </summary>
/// <remarks>
/// Domain events are raised inside an aggregate (<see cref="AggregateRoot{TId}.Raise"/>)
/// and dispatched in-process after <c>SaveChanges</c> succeeds. See
/// <c>Peoplise.Infrastructure</c> for the MediatR-based dispatcher that publishes these
/// as <c>INotification</c>s to any registered handlers within the same process.
/// </remarks>
public interface IDomainEvent
{
}
