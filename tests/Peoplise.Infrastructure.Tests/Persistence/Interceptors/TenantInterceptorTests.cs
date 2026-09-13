using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Peoplise.Infrastructure.Persistence.Interceptors;
using Peoplise.Infrastructure.Tests.TestDoubles;
using Peoplise.SharedKernel.MultiTenancy;
using Xunit;

namespace Peoplise.Infrastructure.Tests.Persistence.Interceptors;

public class TenantInterceptorTests
{
    private static TestDbContext CreateContext(ITenantContext tenantContext) =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new TenantInterceptor(tenantContext))
            .Options);

    [Fact]
    public async Task Sets_TenantId_on_a_newly_added_entity_from_the_tenant_context()
    {
        var tenantGuid = Guid.NewGuid();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantId.From(tenantGuid));

        using var context = CreateContext(tenantContext);
        var aggregate = new TestAggregate(Guid.NewGuid(), "Acme");
        context.TestAggregates.Add(aggregate);

        await context.SaveChangesAsync();

        aggregate.TenantId.Should().Be(tenantGuid);
    }

    [Fact]
    public async Task Throws_when_saving_a_new_entity_with_no_tenant_resolved()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns((TenantId?)null);

        using var context = CreateContext(tenantContext);
        context.TestAggregates.Add(new TestAggregate(Guid.NewGuid(), "Acme"));

        var act = async () => await context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Does_not_restamp_TenantId_on_an_update_even_if_the_tenant_context_later_changes()
    {
        var originalTenantGuid = Guid.NewGuid();
        var laterTenantGuid = Guid.NewGuid();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantId.From(originalTenantGuid));

        using var context = CreateContext(tenantContext);
        var aggregate = new TestAggregate(Guid.NewGuid(), "Acme");
        context.TestAggregates.Add(aggregate);
        await context.SaveChangesAsync();
        aggregate.TenantId.Should().Be(originalTenantGuid);

        // Simulates a later request, on a different tenant, somehow updating a
        // tracked instance of the same row — shouldn't happen given the query filter,
        // but the interceptor itself must not re-stamp on update regardless.
        tenantContext.TenantId.Returns(TenantId.From(laterTenantGuid));
        aggregate.Rename("Acme Renamed");
        await context.SaveChangesAsync();

        aggregate.TenantId.Should().Be(originalTenantGuid, "the interceptor only stamps TenantId on insert, never on update");
    }
}
