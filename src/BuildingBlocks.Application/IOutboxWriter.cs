using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Buffers domain and integration events to be persisted to the outbox.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>
    /// Adds a domain event to the buffer.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    void Add(IDomainEvent domainEvent);

    /// <summary>
    /// Adds an integration event to the buffer.
    /// </summary>
    /// <param name="integrationEvent">The integration event to add.</param>
    void Add(IIntegrationEvent integrationEvent);

    /// <summary>
    /// Removes all buffered events and returns them as outbox messages, one per registered handler of each event.
    /// </summary>
    /// <remarks>
    /// An event with no registered handlers produces no messages.
    /// </remarks>
    /// <returns>The outbox messages for the events that were added since the last flush.</returns>
    IReadOnlyList<OutboxMessage> Flush();
}