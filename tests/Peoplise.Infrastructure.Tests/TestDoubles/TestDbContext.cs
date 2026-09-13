using Microsoft.EntityFrameworkCore;

namespace Peoplise.Infrastructure.Tests.TestDoubles;

/// <summary>
/// A standalone <see cref="DbContext"/> (not <c>AppDbContext</c>) so interceptor tests
/// don't need OpenIddict's model or a resolved tenant/soft-delete query filter — those
/// are tested at the unit level here; <c>AppDbContext</c>'s filter wiring is exercised
/// once real module entities exist to filter, from Stage 2 onward.
/// </summary>
internal sealed class TestDbContext : DbContext
{
    public DbSet<TestAggregate> TestAggregates => Set<TestAggregate>();

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }
}
