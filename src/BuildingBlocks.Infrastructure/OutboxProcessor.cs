using System.Text.Json;

using BuildingBlocks.Application;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Default <see cref="IOutboxProcessor"/> implementation that deserializes pending outbox messages and dispatches them as integration events.
/// </summary>
public sealed class OutboxProcessor : IOutboxProcessor
{
    private readonly IOutboxMessageStore _store;
    private readonly IIntegrationEventDispatcher _dispatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProcessor"/> class.
    /// </summary>
    /// <param name="store">The store used to read and update outbox messages.</param>
    /// <param name="dispatcher">The dispatcher used to publish deserialized integration events.</param>
    public OutboxProcessor(IOutboxMessageStore store, IIntegrationEventDispatcher dispatcher)
    {
        _store = store;
        _dispatcher = dispatcher;
    }

    /// <inheritdoc />
    public async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var pending = await _store.GetPendingAsync(cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                var eventType = Type.GetType(message.Type)
                    ?? throw new InvalidOperationException($"Cannot resolve type '{message.Type}'.");

                var integrationEvent = JsonSerializer.Deserialize(message.Payload, eventType)
                    ?? throw new InvalidOperationException($"Failed to deserialize message '{message.Id}'.");

                var dispatchMethod = typeof(IIntegrationEventDispatcher)
                    .GetMethod(nameof(IIntegrationEventDispatcher.DispatchAsync))!
                    .MakeGenericMethod(eventType);

                await (Task)dispatchMethod.Invoke(_dispatcher, [integrationEvent, cancellationToken])!;

                await _store.MarkProcessedAsync(message.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                await _store.MarkFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }
}