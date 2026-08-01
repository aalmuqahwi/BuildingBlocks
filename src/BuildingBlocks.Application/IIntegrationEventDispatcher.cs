namespace BuildingBlocks.Application;

/// <summary>
/// Dispatches integration events to their registered handlers.
/// </summary>
public interface IIntegrationEventDispatcher
{
    /// <summary>
    /// Dispatches the specified integration event to its registered handlers.
    /// </summary>
    /// <typeparam name="T">The type of integration event.</typeparam>
    /// <param name="integrationEvent">The integration event to dispatch.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task DispatchAsync<T>(T integrationEvent, CancellationToken cancellationToken) where T : IIntegrationEvent;
}