using System.Linq.Expressions;
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

    /// <summary>
    /// Finds every aggregate matching <paramref name="predicate"/> — for batch/system
    /// queries (a scheduled sweep, an admin report) that can't be expressed as a single
    /// by-id lookup. <paramref name="ignoreQueryFilters"/> bypasses the tenant and
    /// soft-delete global query filters when set, for the rare caller (a background job,
    /// not a tenant's own request) that deliberately needs to see across every tenant;
    /// see <c>TenantAwareDbContext</c>'s own tests for why that escape hatch exists.
    /// </summary>
    Task<IReadOnlyList<TAggregateRoot>> ListAsync(
        Expression<Func<TAggregateRoot, bool>> predicate,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default);

    Task AddAsync(TAggregateRoot aggregate, CancellationToken cancellationToken = default);

    void Remove(TAggregateRoot aggregate);
}
