using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Non-generic-event entry point used to dispatch to a strongly-typed domain event handler.
/// </summary>
public interface IDomainEventHandler
{
    /// <summary>
    /// Handles the specified domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Handles domain events of type <typeparamref name="TEvent"/>.
/// </summary>
/// <typeparam name="TEvent">The type of domain event handled.</typeparam>
public interface IDomainEventHandler<TEvent> : IDomainEventHandler
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the specified domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken);

    /// <inheritdoc />
    Task IDomainEventHandler.Handle(IDomainEvent domainEvent, CancellationToken cancellationToken)
        => Handle((TEvent)domainEvent, cancellationToken);
}