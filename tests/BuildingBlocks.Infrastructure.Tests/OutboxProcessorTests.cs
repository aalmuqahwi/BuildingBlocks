using BuildingBlocks.Application;
using BuildingBlocks.Domain;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Tests;

#pragma warning disable CS1591
/// <summary>
/// Committed outbox state, shared across scopes like a real database.
/// </summary>
public class FakeOutboxDatabase
{
    public List<OutboxMessage> Messages { get; } = [];
    public List<Guid> ProcessedIds { get; } = [];
    public List<(Guid Id, string Error, int RetryCount)> Failures { get; } = [];
}

/// <summary>
/// Scoped store and unit of work that stage changes and commit them together, like a DbContext.
/// </summary>
public class FakeOutboxSession : IOutboxMessageStore, IUnitOfWork
{
    private readonly FakeOutboxDatabase _database;
    private readonly List<Action> _staged = [];

    public FakeOutboxSession(FakeOutboxDatabase database) => _database = database;

    public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<OutboxMessage>>(_database.Messages
            .Where(m => !_database.ProcessedIds.Contains(m.Id))
            .ToList());

    public Task AddAsync(IReadOnlyList<OutboxMessage> messages, CancellationToken cancellationToken)
    {
        _staged.Add(() => _database.Messages.AddRange(messages));
        return Task.CompletedTask;
    }

    public Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken)
    {
        _staged.Add(() => _database.ProcessedIds.Add(id));
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(Guid id, string error, int retryCount, CancellationToken cancellationToken)
    {
        _staged.Add(() =>
        {
            _database.Failures.Add((id, error, retryCount));
            var message = _database.Messages.Single(m => m.Id == id);
            message.Error = error;
            message.RetryCount = retryCount;
        });
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var count = _staged.Count;
        _staged.ForEach(change => change());
        _staged.Clear();
        return Task.FromResult(count);
    }
}

public class OrderPlaced : DomainEvent
{
    public Guid OrderId { get; init; }
}

public class OrderPlacedIntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    public Guid OrderId { get; init; }
}

public class OutboxProcessorTests
{
    public class SendConfirmationEmail : IDomainEventHandler<OrderPlaced>
    {
        private readonly List<string> _log;

        public SendConfirmationEmail(List<string> log) => _log = log;

        public Task Handle(OrderPlaced domainEvent, CancellationToken cancellationToken)
        {
            _log.Add($"email:{domainEvent.OrderId}");
            return Task.CompletedTask;
        }
    }

    public class PublishOrderPlaced : IDomainEventHandler<OrderPlaced>
    {
        private readonly IOutboxWriter _outboxWriter;

        public PublishOrderPlaced(IOutboxWriter outboxWriter) => _outboxWriter = outboxWriter;

        public Task Handle(OrderPlaced domainEvent, CancellationToken cancellationToken)
        {
            _outboxWriter.Add(new OrderPlacedIntegrationEvent { OrderId = domainEvent.OrderId });
            return Task.CompletedTask;
        }
    }

    public class PublishThenFail : IDomainEventHandler<OrderPlaced>
    {
        private readonly IOutboxWriter _outboxWriter;

        public PublishThenFail(IOutboxWriter outboxWriter) => _outboxWriter = outboxWriter;

        public Task Handle(OrderPlaced domainEvent, CancellationToken cancellationToken)
        {
            _outboxWriter.Add(new OrderPlacedIntegrationEvent { OrderId = domainEvent.OrderId });
            throw new InvalidOperationException("Failed after publishing.");
        }
    }

    public class FailingHandler : IDomainEventHandler<OrderPlaced>
    {
        public Task Handle(OrderPlaced domainEvent, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Handler-specific failure.");
    }

    public class CancellationObservingHandler : IDomainEventHandler<OrderPlaced>
    {
        public Task Handle(OrderPlaced domainEvent, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    public class NotifyWarehouse :IIntegrationEventHandler<OrderPlacedIntegrationEvent>
    {
        private readonly List<string> _log;

        public NotifyWarehouse(List<string> log) => _log = log;

        public Task Handle(OrderPlacedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            _log.Add($"warehouse:{integrationEvent.OrderId}");
            return Task.CompletedTask;
        }
    }

    private static (IServiceProvider Provider, FakeOutboxDatabase Database, List<string> Log) Build(
        Action<IServiceCollection> registerHandlers)
    {
        var database = new FakeOutboxDatabase();
        var log = new List<string>();

        var services = new ServiceCollection();
        services.AddSingleton(database);
        services.AddSingleton(log);
        services.AddScoped<FakeOutboxSession>();
        services.AddScoped<IOutboxMessageStore>(sp => sp.GetRequiredService<FakeOutboxSession>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<FakeOutboxSession>());
        services.AddOutboxProcessor();
        registerHandlers(services);

        return (services.BuildServiceProvider(), database, log);
    }

    // Simulates the consumer's SaveChanges override: events go through the writer into the database
    private static void Write(IServiceProvider provider, FakeOutboxDatabase database, params object[] events)
    {
        using var scope = provider.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();

        foreach (var @event in events)
        {
            switch (@event)
            {
                case IDomainEvent domainEvent: writer.Add(domainEvent); break;
                case IIntegrationEvent integrationEvent: writer.Add(integrationEvent); break;
            }
        }

        database.Messages.AddRange(writer.Flush());
    }

    private static async Task Process(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IOutboxProcessor>().ProcessPendingAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ProcessPendingAsync_should_deliver_domain_event_to_its_handler_and_mark_processed()
    {
        var (provider, database, log) = Build(s =>
            s.AddScoped<IDomainEventHandler<OrderPlaced>, SendConfirmationEmail>());
        var orderId = Guid.NewGuid();
        Write(provider, database, new OrderPlaced { OrderId = orderId });

        await Process(provider);

        Assert.Equal([$"email:{orderId}"], log);
        Assert.Equal(database.Messages.Select(m => m.Id), database.ProcessedIds);
    }

    [Fact]
    public async Task ProcessPendingAsync_should_retry_only_the_failing_handler()
    {
        var (provider, database, log) = Build(s =>
        {
            s.AddScoped<IDomainEventHandler<OrderPlaced>, SendConfirmationEmail>();
            s.AddScoped<IDomainEventHandler<OrderPlaced>, FailingHandler>();
        });
        Write(provider, database, new OrderPlaced { OrderId = Guid.NewGuid() });

        await Process(provider);
        await Process(provider);

        Assert.Single(log);
        Assert.Single(database.ProcessedIds);
        Assert.Equal([1, 2], database.Failures.Select(f => f.RetryCount));
    }

    [Fact]
    public async Task ProcessPendingAsync_should_commit_events_written_by_a_handler_with_the_processed_mark()
    {
        var (provider, database, log) = Build(s =>
        {
            s.AddScoped<IDomainEventHandler<OrderPlaced>, PublishOrderPlaced>();
            s.AddScoped<IIntegrationEventHandler<OrderPlacedIntegrationEvent>, NotifyWarehouse>();
        });
        var orderId = Guid.NewGuid();
        Write(provider, database, new OrderPlaced { OrderId = orderId });

        await Process(provider);

        var serializer = provider.GetRequiredService<IOutboxSerializer>();
        var written = Assert.Single(database.Messages,
            m => m.Type == serializer.GetTypeName(typeof(OrderPlacedIntegrationEvent)));
        Assert.Equal(serializer.GetTypeName(typeof(NotifyWarehouse)), written.Handler);
        Assert.Single(database.ProcessedIds);

        await Process(provider);

        Assert.Equal([$"warehouse:{orderId}"], log);
        Assert.Equal(2, database.ProcessedIds.Count);
    }

    [Fact]
    public async Task ProcessPendingAsync_should_discard_events_written_by_a_failing_handler()
    {
        var (provider, database, _) = Build(s =>
        {
            s.AddScoped<IDomainEventHandler<OrderPlaced>, PublishThenFail>();
            s.AddScoped<IIntegrationEventHandler<OrderPlacedIntegrationEvent>, NotifyWarehouse>();
        });
        Write(provider, database, new OrderPlaced { OrderId = Guid.NewGuid() });

        await Process(provider);

        Assert.Single(database.Messages);
        Assert.Empty(database.ProcessedIds);
        var failure = Assert.Single(database.Failures);
        Assert.Equal("Failed after publishing.", failure.Error);
    }

    [Fact]
    public async Task ProcessPendingAsync_should_mark_unresolvable_message_failed_and_still_process_the_rest()
    {
        var (provider, database, log) = Build(s =>
            s.AddScoped<IDomainEventHandler<OrderPlaced>, SendConfirmationEmail>());
        var badMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "NonExistent.Type, NonExistent.Assembly",
            Handler = "NonExistent.Handler, NonExistent.Assembly",
            Payload = "{}",
            OccurredOnUtc = DateTime.UtcNow
        };
        database.Messages.Add(badMessage);
        Write(provider, database, new OrderPlaced { OrderId = Guid.NewGuid() });

        await Process(provider);

        Assert.Equal(badMessage.Id, Assert.Single(database.Failures).Id);
        Assert.Single(log);
        Assert.Single(database.ProcessedIds);
    }

    [Fact]
    public async Task ProcessPendingAsync_should_fail_message_whose_handler_is_no_longer_registered()
    {
        var (provider, database, _) = Build(_ => { });
        var serializer = provider.GetRequiredService<IOutboxSerializer>();
        database.Messages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = serializer.GetTypeName(typeof(OrderPlaced)),
            Handler = serializer.GetTypeName(typeof(SendConfirmationEmail)),
            Payload = serializer.Serialize(new OrderPlaced()),
            OccurredOnUtc = DateTime.UtcNow
        });

        await Process(provider);

        Assert.Contains("is not registered", Assert.Single(database.Failures).Error);
    }

    [Fact]
    public async Task ProcessPendingAsync_should_store_the_handler_exception_message_and_increment_retry_count()
    {
        var (provider, database, _) = Build(s =>
            s.AddScoped<IDomainEventHandler<OrderPlaced>, FailingHandler>());
        Write(provider, database, new OrderPlaced());
        database.Messages[0].RetryCount = 2;

        await Process(provider);

        var failure = Assert.Single(database.Failures);
        Assert.Equal("Handler-specific failure.", failure.Error);
        Assert.Equal(3, failure.RetryCount);
    }

    [Fact]
    public async Task ProcessPendingAsync_should_not_mark_failed_when_cancelled()
    {
        var (provider, database, _) = Build(s =>
            s.AddScoped<IDomainEventHandler<OrderPlaced>, CancellationObservingHandler>());
        Write(provider, database, new OrderPlaced());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        using var scope = provider.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        await Assert.ThrowsAsync<OperationCanceledException>(() => processor.ProcessPendingAsync(cts.Token));

        Assert.Empty(database.Failures);
        Assert.Empty(database.ProcessedIds);
    }
}
#pragma warning restore CS1591