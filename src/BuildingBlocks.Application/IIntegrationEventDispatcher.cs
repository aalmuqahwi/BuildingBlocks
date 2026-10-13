namespace BuildingBlocks.Application;

/// <summary>
/// Dispatches integration events to their registered handlers.
/// </summary>
public interface IIntegrationEventDispatcher
{
    /// <summary>
    /// Dispatches the specified integration event to its registered handlers.
    /// </summary>
    /// <param name="integrationEvent">The integration event to dispatch.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task DispatchAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}