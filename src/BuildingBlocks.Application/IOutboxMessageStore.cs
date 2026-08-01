namespace BuildingBlocks.Application;

/// <summary>
/// Provides persistence for outbox messages.
/// </summary>
public interface IOutboxMessageStore
{
    /// <summary>
    /// Gets the outbox messages that have not yet been processed.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The pending outbox messages.</returns>
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Marks the outbox message with the specified identifier as processed.
    /// </summary>
    /// <param name="id">The identifier of the outbox message.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Marks the outbox message with the specified identifier as failed.
    /// </summary>
    /// <param name="id">The identifier of the outbox message.</param>
    /// <param name="error">A description of the error that caused processing to fail.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken);
}