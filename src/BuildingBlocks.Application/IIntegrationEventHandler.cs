namespace BuildingBlocks.Application;

/// <summary>
/// Non-generic-event entry point used to dispatch to a strongly-typed integration event handler.
/// </summary>
public interface IIntegrationEventHandler
{
    /// <summary>
    /// Handles the specified integration event.
    /// </summary>
    /// <param name="integrationEvent">The integration event to handle.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task Handle(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Handles integration events of type <typeparamref name="TEvent"/>.
/// </summary>
/// <typeparam name="TEvent">The type of integration event handled.</typeparam>
public interface IIntegrationEventHandler<TEvent> : IIntegrationEventHandler
    where TEvent : IIntegrationEvent
{
    /// <summary>
    /// Handles the specified integration event.
    /// </summary>
    /// <param name="integrationEvent">The integration event to handle.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task Handle(TEvent integrationEvent, CancellationToken cancellationToken);

    /// <inheritdoc />
    Task IIntegrationEventHandler.Handle(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        => Handle((TEvent)integrationEvent, cancellationToken);
}