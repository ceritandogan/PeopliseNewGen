namespace Peoplise.SharedKernel.Domain;

/// <summary>
/// Non-generic view of <see cref="AggregateRoot{TId}"/>'s pending domain events. Because
/// <see cref="AggregateRoot{TId}"/> is generic over its id type, EF Core's change tracker
/// can't enumerate "all tracked aggregates" directly (there's no way to write
/// <c>Entries&lt;AggregateRoot&lt;?&gt;&gt;()</c> in C#). The unit of work enumerates
/// <see cref="IHasDomainEvents"/> instead, which every aggregate implements regardless
/// of its id type.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
