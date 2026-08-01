namespace BuildingBlocks.Application;

/// <summary>
/// Handles integration events of type <typeparamref name="TEvent"/>.
/// </summary>
/// <typeparam name="TEvent">The type of integration event handled.</typeparam>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    /// <summary>
    /// Handles the specified integration event.
    /// </summary>
    /// <param name="integrationEvent">The integration event to handle.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task Handle(TEvent integrationEvent, CancellationToken cancellationToken);
}