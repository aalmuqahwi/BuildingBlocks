namespace BuildingBlocks.Application;

/// <summary>
/// Represents a persisted event awaiting or having completed delivery to a single handler.
/// </summary>
/// <remarks>
/// An event with several handlers is persisted as one message per handler, so each handler succeeds, fails, and retries independently.
/// </remarks>
public sealed class OutboxMessage
{
    /// <summary>
    /// Gets or sets the unique identifier of the message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the type name of the event, as produced by <see cref="IOutboxSerializer.GetTypeName"/>.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type name of the handler the message is delivered to, as produced by <see cref="IOutboxSerializer.GetTypeName"/>.
    /// </summary>
    public string Handler { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized event payload.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC date and time at which the event occurred.
    /// </summary>
    public DateTime OccurredOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time at which the message was processed, or <see langword="null"/> if it has not been processed yet.
    /// </summary>
    public DateTime? ProcessedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the error message from the most recent failed processing attempt, if any.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the number of times processing of the message has been retried.
    /// </summary>
    public int RetryCount { get; set; }
}