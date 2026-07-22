using BuildingBlocks.Application;
using BuildingBlocks.Domain;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Tests;

#pragma warning disable CS1591
public class DomainEventDispatcherServiceExtensionsTests
{
    public class TestDomainEventForDispatch : DomainEvent;

    public class HandlerOne : IDomainEventHandler<TestDomainEventForDispatch>
    {
        private readonly List<string> _log;

        public HandlerOne(List<string> log) => _log = log;

        public Task Handle(TestDomainEventForDispatch domainEvent, CancellationToken cancellationToken)
        {
            _log.Add("HandlerOne - handled");
            return Task.CompletedTask;
        }
    }

    public class HandlerTwo : IDomainEventHandler<TestDomainEventForDispatch>
    {
        private readonly List<string> _log;

        public HandlerTwo(List<string> log) => _log = log;

        public Task Handle(TestDomainEventForDispatch domainEvent, CancellationToken cancellationToken)
        {
            _log.Add("HandlerTwo - handled");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task AddDomainEventDispatcher_should_invoke_all_registered_handlers()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddDomainEventDispatcher(typeof(DomainEventDispatcherServiceExtensionsTests).Assembly);
        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();

        await dispatcher.DispatchAsync(new TestDomainEventForDispatch(), CancellationToken.None);

        Assert.Equal(2, log.Count);
        Assert.Contains("HandlerOne - handled", log);
        Assert.Contains("HandlerTwo - handled", log);
    }

}
#pragma warning restore CS1591