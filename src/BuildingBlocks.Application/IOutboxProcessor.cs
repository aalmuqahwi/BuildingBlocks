namespace BuildingBlocks.Application;

/// <summary>
/// Processes pending outbox messages by delivering their events to the handlers they were written for.
/// </summary>
public interface IOutboxProcessor
{
    /// <summary>
    /// Processes all pending outbox messages.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task ProcessPendingAsync(CancellationToken cancellationToken);
}