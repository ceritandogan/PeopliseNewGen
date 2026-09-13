using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Tests.TestDoubles;
using Xunit;

namespace Peoplise.Infrastructure.Tests.Persistence;

/// <summary>
/// Proves <see cref="Peoplise.Infrastructure.Persistence.TenantAwareDbContext"/>'s
/// reflection-driven global query filter actually filters — the concern this class
/// exists for isn't obvious from the interceptor tests (which never issue a query) or
/// from a successful build (a wrong filter still compiles).
/// </summary>
public class TenantAwareDbContextTests
{
    private static MultiTenantTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MultiTenantTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Only_returns_rows_belonging_to_the_current_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        using var context = CreateContext();
        context.TestAggregates.AddRange(
            new TestAggregate(Guid.NewGuid(), "A's widget") { TenantId = tenantA },
            new TestAggregate(Guid.NewGuid(), "B's widget") { TenantId = tenantB });
        await context.SaveChangesAsync();

        context.Tenant = tenantA;
        var visible = await context.TestAggregates.ToListAsync();

        visible.Should().ContainSingle(e => e.Name == "A's widget");
    }

    [Fact]
    public async Task Returns_no_rows_when_no_tenant_is_resolved()
    {
        using var context = CreateContext();
        context.TestAggregates.Add(new TestAggregate(Guid.NewGuid(), "Acme") { TenantId = Guid.NewGuid() });
        await context.SaveChangesAsync();

        context.Tenant = null;
        var visible = await context.TestAggregates.ToListAsync();

        visible.Should().BeEmpty("no tenant resolved means fail closed, not fail open");
    }

    [Fact]
    public async Task The_filter_re_evaluates_per_query_not_once_at_model_build_time()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        using var context = CreateContext();
        context.TestAggregates.AddRange(
            new TestAggregate(Guid.NewGuid(), "A's widget") { TenantId = tenantA },
            new TestAggregate(Guid.NewGuid(), "B's widget") { TenantId = tenantB });
        await context.SaveChangesAsync();

        context.Tenant = tenantA;
        (await context.TestAggregates.ToListAsync()).Should().ContainSingle(e => e.Name == "A's widget");

        // Same context instance, same already-built model — switching the tenant must
        // still change what the very next query returns.
        context.Tenant = tenantB;
        (await context.TestAggregates.ToListAsync()).Should().ContainSingle(e => e.Name == "B's widget");
    }

    [Fact]
    public async Task Excludes_soft_deleted_rows_by_default()
    {
        var tenant = Guid.NewGuid();

        using var context = CreateContext();
        var aggregate = new TestAggregate(Guid.NewGuid(), "Acme") { TenantId = tenant };
        context.TestAggregates.Add(aggregate);
        await context.SaveChangesAsync();

        aggregate.IsDeleted = true;
        await context.SaveChangesAsync();

        context.Tenant = tenant;
        var visible = await context.TestAggregates.ToListAsync();

        visible.Should().BeEmpty();
    }

    [Fact]
    public async Task IgnoreQueryFilters_bypasses_both_the_tenant_and_soft_delete_filter()
    {
        var tenant = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();

        using var context = CreateContext();
        var deleted = new TestAggregate(Guid.NewGuid(), "Deleted") { TenantId = tenant, IsDeleted = true };
        var otherTenants = new TestAggregate(Guid.NewGuid(), "Other tenant's") { TenantId = otherTenant };
        context.TestAggregates.AddRange(deleted, otherTenants);
        await context.SaveChangesAsync();

        context.Tenant = tenant;
        var all = await context.TestAggregates.IgnoreQueryFilters().ToListAsync();

        all.Should().HaveCount(2, "an explicit IgnoreQueryFilters() call is the escape hatch for admin/background tooling");
    }
}
