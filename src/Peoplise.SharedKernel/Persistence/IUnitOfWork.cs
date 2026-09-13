namespace Peoplise.SharedKernel.Persistence;

/// <summary>
/// Commits every change made through this request's repositories as one transaction,
/// then dispatches the domain events those aggregates raised. Application command
/// handlers depend on this instead of any specific repository's save method, so a
/// handler can coordinate several aggregates and still commit them together.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes and dispatches the domain events raised by any
    /// aggregate tracked in this unit of work. Returns the number of state entries
    /// written to the store.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
