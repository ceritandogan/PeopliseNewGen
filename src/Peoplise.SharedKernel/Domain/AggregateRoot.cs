namespace Peoplise.SharedKernel.Domain;

/// <summary>
/// Base class for an aggregate root: the single entry point through which a cluster of
/// entities and value objects is loaded, mutated, and persisted as one consistency
/// boundary. Only aggregate roots are held by <see cref="Persistence.IRepository{TAggregateRoot,TId}"/>.
/// </summary>
/// <typeparam name="TId">The type of the aggregate's identity value.</typeparam>
public abstract class AggregateRoot<TId> : BaseEntity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Domain events raised by this aggregate since it was loaded, pending dispatch.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Records that <paramref name="domainEvent"/> happened as a result of a state
    /// change on this aggregate. The event is dispatched after the unit of work
    /// commits successfully — raising it does not publish it immediately.
    /// </summary>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Removes all pending domain events. Called by the infrastructure layer once the
    /// events have been dispatched, so they are not published twice.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
