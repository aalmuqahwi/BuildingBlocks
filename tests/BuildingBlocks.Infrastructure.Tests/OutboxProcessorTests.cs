using System.Text.Json;

using BuildingBlocks.Application;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Tests;


#pragma warning disable CS1591
public class OutboxProcessorTests
{
    public class FakeOutboxMessageStore : IOutboxMessageStore
    {
        private readonly List<OutboxMessage> _pending;

        public List<Guid> ProcessedIds { get; } = [];
        public List<(Guid Id, string Error, int RetryCount)> FailedIds { get; } = [];

        public FakeOutboxMessageStore(List<OutboxMessage> pending) => _pending = pending;

        public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<OutboxMessage>>(_pending);

        public Task MarkFailedAsync(Guid id, string error, int retryCount, CancellationToken cancellationToken)
        {
            FailedIds.Add((id, error, retryCount));
            return Task.CompletedTask;
        }

        public Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken)
        {
            ProcessedIds.Add(id);
            return Task.CompletedTask;
        }
    }

    public class ThrowingHandler : IIntegrationEventHandler<TestIntegrationEvent>
    {
        public Task Handle(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Handler-specific failure.");
    }

    [Fact]
    public async Task ProcessPendingAsync_should_mark_bad_message_failed_and_still_process_good_message()
    {
        var goodEvent = new TestIntegrationEvent();
        var goodMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(TestIntegrationEvent).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(goodEvent),
            OccurredOnUtc = DateTime.UtcNow
        };

        var badMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "NonExistent.Type.ThatDoesNotExist",
            Payload = "{}",
            OccurredOnUtc = DateTime.UtcNow
        };

        var store = new FakeOutboxMessageStore([badMessage, goodMessage]);

        var services = new ServiceCollection();
        services.AddSingleton<IOutboxMessageStore>(store);
        services.AddOutboxProcessor();

        var provider = services.BuildServiceProvider();
        var processor = provider.GetRequiredService<IOutboxProcessor>();

        await processor.ProcessPendingAsync(CancellationToken.None);

        Assert.Contains(badMessage.Id, store.FailedIds.Select(f => f.Id));
        Assert.Contains(goodMessage.Id, store.ProcessedIds);
        Assert.Equal(1, store.FailedIds.Single(f => f.Id == badMessage.Id).RetryCount);
    }

    [Fact]
    public async Task ProcessPendingAsync_should_store_the_handler_exception_message_and_increment_retry_count()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(TestIntegrationEvent).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new TestIntegrationEvent()),
            OccurredOnUtc = DateTime.UtcNow,
            RetryCount = 2
        };

        var store = new FakeOutboxMessageStore([message]);

        var services = new ServiceCollection();
        services.AddSingleton<IOutboxMessageStore>(store);
        services.AddOutboxProcessor();
        services.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>, ThrowingHandler>();

        var provider = services.BuildServiceProvider();
        var processor = provider.GetRequiredService<IOutboxProcessor>();

        await processor.ProcessPendingAsync(CancellationToken.None);

        var failure = Assert.Single(store.FailedIds);
        Assert.Equal("Handler-specific failure.", failure.Error);
        Assert.Equal(3, failure.RetryCount);
    }
}
#pragma warning restore CS1591