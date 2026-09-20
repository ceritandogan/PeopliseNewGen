using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Peoplise.SharedKernel.Domain;
using Peoplise.SharedKernel.Persistence;

namespace Peoplise.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IRepository{TAggregateRoot,TId}"/> shared by
/// every aggregate type. A module only needs this generic implementation registered for
/// its aggregate; it does not need a hand-written repository class unless its queries
/// grow beyond simple by-id lookup.
/// </summary>
public sealed class GenericRepository<TAggregateRoot, TId> : IRepository<TAggregateRoot, TId>
    where TAggregateRoot : AggregateRoot<TId>
    where TId : notnull
{
    private readonly AppDbContext _context;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<TAggregateRoot?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        _context.Set<TAggregateRoot>()
            .SingleOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);

    public async Task<IReadOnlyList<TAggregateRoot>> ListAsync(
        Expression<Func<TAggregateRoot, bool>> predicate,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TAggregateRoot>().AsQueryable();
        if (ignoreQueryFilters)
            query = query.IgnoreQueryFilters();

        // Unlike GetByIdAsync (one row), this can return many aggregates at once, each
        // with its own owned collections auto-included by EF Core. SingleQuery mode
        // cross-joins every owned collection into one flattened result set — fine for
        // one row, but caught live for multiple rows with unevenly-populated
        // collections (some with data, some with none): EF Core mis-grouped which
        // joined row belonged to which collection and threw reading a NULL key column
        // as a non-nullable Guid. AsSplitQuery avoids the cartesian join entirely, at
        // the cost of one query per collection instead of one query total — the right
        // tradeoff for a background sweep, not a hot request path.
        return await query.Where(predicate).AsSplitQuery().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TAggregateRoot aggregate, CancellationToken cancellationToken = default) =>
        await _context.Set<TAggregateRoot>().AddAsync(aggregate, cancellationToken);

    public void Remove(TAggregateRoot aggregate) => _context.Set<TAggregateRoot>().Remove(aggregate);
}
