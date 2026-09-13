using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Peoplise.Infrastructure.Events;
using Peoplise.Infrastructure.Tests.TestDoubles;
using Xunit;

namespace Peoplise.Infrastructure.Tests.Events;

public class MediatRDomainEventDispatcherTests
{
    /// <summary>Collects handled events without relying on shared static state across tests.</summary>
    private sealed class EventCollector
    {
        public List<TestAggregateRenamedEvent> Handled { get; } = [];
    }

    private sealed class TestAggregateRenamedEventHandler : INotificationHandler<DomainEventNotification<TestAggregateRenamedEvent>>
    {
        private readonly EventCollector _collector;

        public TestAggregateRenamedEventHandler(EventCollector collector) => _collector = collector;

        public Task Handle(DomainEventNotification<TestAggregateRenamedEvent> notification, CancellationToken cancellationToken)
        {
            _collector.Handled.Add(notification.DomainEvent);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Dispatches_a_raised_domain_event_to_its_registered_handler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<EventCollector>();
        services.AddSingleton<INotificationHandler<DomainEventNotification<TestAggregateRenamedEvent>>, TestAggregateRenamedEventHandler>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DomainEventNotification<>).Assembly));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new MediatRDomainEventDispatcher(provider.GetRequiredService<IPublisher>());

        var domainEvent = new TestAggregateRenamedEvent(Guid.NewGuid(), "New Name");

        await dispatcher.DispatchAsync([domainEvent]);

        provider.GetRequiredService<EventCollector>().Handled.Should().ContainSingle()
            .Which.Should().Be(domainEvent);
    }

    [Fact]
    public async Task An_event_with_no_registered_handler_is_dispatched_without_throwing()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DomainEventNotification<>).Assembly));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new MediatRDomainEventDispatcher(provider.GetRequiredService<IPublisher>());

        var act = async () => await dispatcher.DispatchAsync([new TestAggregateRenamedEvent(Guid.NewGuid(), "New Name")]);

        await act.Should().NotThrowAsync();
    }
}
