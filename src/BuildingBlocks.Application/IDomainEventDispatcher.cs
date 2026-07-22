using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Dispatches domain events to their registered handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches the specified domain event to its registered handlers.
    /// </summary>
    /// <param name="domainEvent">The domain event to dispatch.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}