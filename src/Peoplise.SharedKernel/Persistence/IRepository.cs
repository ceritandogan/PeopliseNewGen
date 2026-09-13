using Peoplise.SharedKernel.Domain;

namespace Peoplise.SharedKernel.Persistence;

/// <summary>
/// Persistence abstraction for one aggregate root type. Repositories only ever load,
/// add, or remove whole aggregates — never the entities nested inside them — so that
/// the aggregate's invariants stay enforced by its own methods.
/// </summary>
/// <typeparam name="TAggregateRoot">The aggregate root type this repository manages.</typeparam>
/// <typeparam name="TId">The type of the aggregate's identity value.</typeparam>
public interface IRepository<TAggregateRoot, in TId>
    where TAggregateRoot : AggregateRoot<TId>
    where TId : notnull
{
    Task<TAggregateRoot?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task AddAsync(TAggregateRoot aggregate, CancellationToken cancellationToken = default);

    void Remove(TAggregateRoot aggregate);
}
