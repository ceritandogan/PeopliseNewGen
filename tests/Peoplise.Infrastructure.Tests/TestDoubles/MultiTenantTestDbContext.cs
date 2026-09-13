using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;

namespace Peoplise.Infrastructure.Tests.TestDoubles;

/// <summary>
/// Exercises <see cref="TenantAwareDbContext"/>'s actual filter-application code (the
/// same base class <c>AppDbContext</c> derives from) against a real entity and real
/// queries, via a test-settable tenant instead of <c>ITenantContext</c>. Stage 1's
/// <c>AppDbContext</c> has no concrete entities yet, so this is the only place that
/// proves the reflection-driven filter wiring behaves correctly before any module
/// entity exists to test it against.
/// </summary>
internal sealed class MultiTenantTestDbContext : TenantAwareDbContext
{
    /// <summary>Set directly per test — stands in for a resolved <c>ITenantContext</c>.</summary>
    public Guid? Tenant { get; set; }

    protected override Guid? CurrentTenantId => Tenant;

    public DbSet<TestAggregate> TestAggregates => Set<TestAggregate>();

    public MultiTenantTestDbContext(DbContextOptions<MultiTenantTestDbContext> options) : base(options)
    {
    }
}
