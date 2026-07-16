namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for domain events, capturing when the event occurred.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    /// <summary>
    /// Gets the UTC date and time at which the event occurred.
    /// </summary>
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}