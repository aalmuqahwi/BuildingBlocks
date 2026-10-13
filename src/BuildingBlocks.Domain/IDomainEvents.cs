namespace BuildingBlocks.Domain;

/// <summary>
/// Represents something significant that happened within the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the UTC date and time at which the event occurred.
    /// </summary>
    DateTime OccurredOnUtc { get; }
}