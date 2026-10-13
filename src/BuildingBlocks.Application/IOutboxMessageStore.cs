namespace BuildingBlocks.Application;

/// <summary>
/// Provides persistence for outbox messages.
/// </summary>
/// <remarks>
/// Write methods only stage their changes; they are committed by <see cref="IUnitOfWork.SaveChangesAsync"/>
/// resolved from the same scope. This lets <see cref="IOutboxProcessor"/> commit a handler's work, the messages it wrote,
/// and the processed mark in a single transaction.
/// </remarks>
public interface IOutboxMessageStore
{
    /// <summary>
    /// Gets the outbox messages that have not yet been processed.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The pending outbox messages.</returns>
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stages the specified outbox messages to be added.
    /// </summary>
    /// <param name="messages">The outbox messages to add.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task AddAsync(IReadOnlyList<OutboxMessage> messages, CancellationToken cancellationToken);

    /// <summary>
    /// Stages the outbox message with the specified identifier to be marked as processed.
    /// </summary>
    /// <param name="id">The identifier of the outbox message.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Stages the outbox message with the specified identifier to be marked as failed.
    /// </summary>
    /// <param name="id">The identifier of the outbox message.</param>
    /// <param name="error">A description of the error that caused processing to fail.</param>
    /// <param name="retryCount">The total number of processing attempts made for the message so far, including this one.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task MarkFailedAsync(Guid id, string error, int retryCount, CancellationToken cancellationToken);
}