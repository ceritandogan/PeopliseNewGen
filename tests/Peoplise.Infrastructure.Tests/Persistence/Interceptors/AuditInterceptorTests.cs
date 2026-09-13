using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Peoplise.Infrastructure.Persistence.Interceptors;
using Peoplise.Infrastructure.Tests.TestDoubles;
using Peoplise.SharedKernel.Auditing;
using Xunit;

namespace Peoplise.Infrastructure.Tests.Persistence.Interceptors;

public class AuditInterceptorTests
{
    private static TestDbContext CreateContext(ICurrentUserContext currentUser) =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditInterceptor(currentUser))
            .Options);

    [Fact]
    public async Task Stamps_CreatedAt_and_CreatedBy_on_insert()
    {
        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns("user-1");

        using var context = CreateContext(currentUser);
        var aggregate = new TestAggregate(Guid.NewGuid(), "Acme");
        context.TestAggregates.Add(aggregate);

        var before = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        aggregate.CreatedBy.Should().Be("user-1");
        aggregate.CreatedAt.Should().BeOnOrAfter(before);
        aggregate.UpdatedAt.Should().BeNull();
        aggregate.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public async Task Stamps_UpdatedAt_and_UpdatedBy_on_modify_without_touching_CreatedAt()
    {
        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns("creator");

        using var context = CreateContext(currentUser);
        var aggregate = new TestAggregate(Guid.NewGuid(), "Acme");
        context.TestAggregates.Add(aggregate);
        await context.SaveChangesAsync();
        var createdAt = aggregate.CreatedAt;

        currentUser.UserId.Returns("editor");
        aggregate.Rename("Acme Renamed");
        await context.SaveChangesAsync();

        aggregate.CreatedBy.Should().Be("creator");
        aggregate.CreatedAt.Should().Be(createdAt);
        aggregate.UpdatedBy.Should().Be("editor");
        aggregate.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Converts_a_removed_entity_into_a_soft_delete_instead_of_a_physical_one()
    {
        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns("user-1");

        using var context = CreateContext(currentUser);
        var aggregate = new TestAggregate(Guid.NewGuid(), "Acme");
        context.TestAggregates.Add(aggregate);
        await context.SaveChangesAsync();

        context.TestAggregates.Remove(aggregate);
        await context.SaveChangesAsync();

        aggregate.IsDeleted.Should().BeTrue();
        (await context.TestAggregates.CountAsync()).Should().Be(1, "the row must still exist — only IsDeleted flips");
    }
}
