namespace BuildingBlocks.Application;

/// <summary>
/// Processes pending outbox messages by dispatching their integration events.
/// </summary>
public interface IOutboxProcessor
{
    /// <summary>
    /// Processes all pending outbox messages.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task ProcessPendingAsync(CancellationToken cancellationToken);
}