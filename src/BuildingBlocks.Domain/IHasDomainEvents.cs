namespace BuildingBlocks.Domain;

/// <summary>
/// Represents an object that records domain events.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Removes and returns all domain events currently recorded on the object.
    /// </summary>
    /// <returns>The domain events that were recorded since the last pop.</returns>
    IReadOnlyList<IDomainEvent> PopDomainEvents();
}