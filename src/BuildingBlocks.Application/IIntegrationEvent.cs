namespace BuildingBlocks.Application;

/// <summary>
/// Represents an event published across service boundaries.
/// </summary>
public interface IIntegrationEvent
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