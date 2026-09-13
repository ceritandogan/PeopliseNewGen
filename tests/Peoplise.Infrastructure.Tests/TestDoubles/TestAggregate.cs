using Peoplise.SharedKernel.Auditing;
using Peoplise.SharedKernel.Domain;
using Peoplise.SharedKernel.MultiTenancy;

namespace Peoplise.Infrastructure.Tests.TestDoubles;

/// <summary>
/// A minimal aggregate implementing every marker interface the interceptors and query
/// filters key off of, used only to exercise infrastructure plumbing without depending
/// on any real module's domain model (none exists yet at Stage 1).
/// </summary>
public sealed class TestAggregate : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string Name { get; private set; } = string.Empty;

    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    private TestAggregate()
    {
        // Reserved for EF Core materialization.
    }

    public TestAggregate(Guid id, string name) : base(id)
    {
        Name = name;
    }

    public void Rename(string newName)
    {
        Name = newName;
        Raise(new TestAggregateRenamedEvent(Id, newName));
    }
}

public sealed record TestAggregateRenamedEvent(Guid AggregateId, string NewName) : IDomainEvent;
