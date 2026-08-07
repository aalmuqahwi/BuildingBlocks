using BuildingBlocks.Application;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Tests;

#pragma warning disable CS1591
public record TestIntegrationEvent : IIntegrationEvent
{
    public Guid Id => Guid.NewGuid();
    public DateTime OccurredOn => DateTime.UtcNow;
}

public class IntegrationEventDispatcherTests
{
    public class IntegrationHandlerOne : IIntegrationEventHandler<TestIntegrationEvent>
    {
        private readonly List<string> _log;

        public IntegrationHandlerOne(List<string> log) => _log = log;

        public Task Handle(TestIntegrationEvent integrationEvent, CancellationToken ct)
        {
            _log.Add("IntegrationHandlerOne - handled");
            return Task.CompletedTask;
        }
    }

    public class IntegrationHandlerTwo : IIntegrationEventHandler<TestIntegrationEvent>
    {
        private readonly List<string> _log;

        public IntegrationHandlerTwo(List<string> log) => _log = log;

        public Task Handle(TestIntegrationEvent integrationEvent, CancellationToken ct)
        {
            _log.Add("IntegrationHandlerTwo - handled");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task IntegrationEventDispatcher_should_invoke_all_registered_handlers()
    {
        var log = new List<string>();

        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddOutboxProcessor();
        services.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, IntegrationHandlerOne>();
        services.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, IntegrationHandlerTwo>();
        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IIntegrationEventDispatcher>();

        await dispatcher.DispatchAsync(new TestIntegrationEvent(), CancellationToken.None);

        Assert.Contains("IntegrationHandlerOne - handled", log);
        Assert.Contains("IntegrationHandlerTwo - handled", log);
    }
}
#pragma warning restore CS1591