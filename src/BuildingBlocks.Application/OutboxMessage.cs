namespace BuildingBlocks.Application;

/// <summary>
/// Represents a persisted integration event awaiting or having completed dispatch.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// Gets or sets the unique identifier of the message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the assembly-qualified type name of the integration event.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized integration event payload.
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