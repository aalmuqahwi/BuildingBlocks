namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for domain events, capturing a unique identifier and when the event occurred.
/// </summary>
/// <remarks>
/// Both properties are <see langword="init"/>-settable so the original values survive serialization round trips,
/// such as when the event is persisted to and read back from an outbox.
/// </remarks>
public abstract class DomainEvent : IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the UTC date and time at which the event occurred.
    /// </summary>
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}