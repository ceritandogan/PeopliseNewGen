using Peoplise.SharedKernel.Domain;
using Peoplise.SharedKernel.Events;
using Peoplise.SharedKernel.Persistence;

namespace Peoplise.Infrastructure.Persistence;

/// <summary>
/// Commits the current <see cref="AppDbContext"/>'s pending changes, then dispatches the
/// domain events raised by whatever aggregates were part of that commit. Events are
/// captured and cleared before dispatch so a re-entrant save during a handler can't
/// re-publish the same event.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly IDomainEventDispatcher _dispatcher;

    public UnitOfWork(AppDbContext context, IDomainEventDispatcher dispatcher)
    {
        _context = context;
        _dispatcher = dispatcher;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregatesWithEvents = _context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        var result = await _context.SaveChangesAsync(cancellationToken);

        var domainEvents = aggregatesWithEvents.SelectMany(a => a.DomainEvents).ToList();
        foreach (var aggregate in aggregatesWithEvents)
            aggregate.ClearDomainEvents();

        await _dispatcher.DispatchAsync(domainEvents, cancellationToken);

        return result;
    }
}
