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

    public async Task AddAsync(TAggregateRoot aggregate, CancellationToken cancellationToken = default) =>
        await _context.Set<TAggregateRoot>().AddAsync(aggregate, cancellationToken);

    public void Remove(TAggregateRoot aggregate) => _context.Set<TAggregateRoot>().Remove(aggregate);
}
