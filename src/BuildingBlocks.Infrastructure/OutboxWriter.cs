using BuildingBlocks.Application;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Default <see cref="IOutboxWriter"/> implementation that buffers integration events in memory.
/// </summary>
internal sealed class OutboxWriter : IOutboxWriter
{
    private readonly List<IIntegrationEvent> _events = [];

    /// <inheritdoc />
    public void Add(IIntegrationEvent integrationEvent)
        => _events.Add(integrationEvent);

    /// <inheritdoc />
    public IReadOnlyList<IIntegrationEvent> Flush()
    {
        var snapshot = _events.ToList();
        _events.Clear();
        return snapshot;
    }
}