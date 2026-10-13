using BuildingBlocks.Application;
using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Default <see cref="IOutboxWriter"/> implementation that buffers events in memory
/// and fans each one out to a message per registered handler on flush.
/// </summary>
internal sealed class OutboxWriter : IOutboxWriter
{
    private readonly OutboxHandlerRegistry _registry;
    private readonly IOutboxSerializer _serializer;
    private readonly List<(object Event, DateTime OccurredOnUtc)> _events = [];

    public OutboxWriter(OutboxHandlerRegistry registry, IOutboxSerializer serializer)
    {
        _registry = registry;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public void Add(IDomainEvent domainEvent)
        => _events.Add((domainEvent, domainEvent.OccurredOnUtc));

    /// <inheritdoc />
    public void Add(IIntegrationEvent integrationEvent)
        => _events.Add((integrationEvent, integrationEvent.OccurredOnUtc));

    /// <inheritdoc />
    public IReadOnlyList<OutboxMessage> Flush()
    {
        var messages = new List<OutboxMessage>();

        foreach (var (@event, occurredOnUtc) in _events)
        {
            var eventType = @event.GetType();
            var typeName = _serializer.GetTypeName(eventType);
            var payload = _serializer.Serialize(@event);

            foreach (var handlerType in _registry.GetHandlerTypes(eventType))
            {
                messages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = typeName,
                    Handler = _serializer.GetTypeName(handlerType),
                    Payload = payload,
                    OccurredOnUtc = occurredOnUtc
                });
            }
        }

        _events.Clear();
        return messages;
    }
}