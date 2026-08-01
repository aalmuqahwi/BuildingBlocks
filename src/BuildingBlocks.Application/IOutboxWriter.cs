namespace BuildingBlocks.Application;

/// <summary>
/// Buffers integration events to be persisted to the outbox.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>
    /// Adds an integration event to the buffer.
    /// </summary>
    /// <param name="integrationEvent">The integration event to add.</param>
    void Add(IIntegrationEvent integrationEvent);

    /// <summary>
    /// Removes and returns all integration events currently buffered.
    /// </summary>
    /// <returns>The integration events that were added since the last flush.</returns>
    IReadOnlyList<IIntegrationEvent> Flush();
}