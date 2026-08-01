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
    /// Gets the date and time at which the event occurred.
    /// </summary>
    DateTime OccurredOn { get; }
}