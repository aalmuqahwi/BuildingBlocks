namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for entities identified by an <typeparamref name="TId"/> value.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class Entity<TId> : IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TId}"/> class.
    /// </summary>
    /// <param name="id">The entity's identifier.</param>
    protected Entity(TId id) => Id = id;

    /// <summary>
    /// Gets the entity's identifier.
    /// </summary>
    public TId Id { get; }

    /// <summary>
    /// Removes and returns all domain events currently recorded on the entity.
    /// </summary>
    /// <returns>The domain events that were recorded since the last pop.</returns>
    public IReadOnlyList<IDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }

    /// <summary>
    /// Records a domain event to be returned by the next call to <see cref="PopDomainEvents"/>.
    /// </summary>
    /// <param name="domainEvent">The domain event to record.</param>
    public void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Determines whether the specified object is an entity of the same type with the same <see cref="Id"/>.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns><see langword="true"/> if the specified object is equal to the current entity; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id.Equals(other.Id);
    }

    /// <summary>
    /// Returns a hash code based on the entity's <see cref="Id"/>.
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Determines whether two entities are equal.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns><see langword="true"/> if the entities are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => left is null ? right is null : left.Equals(right);

    /// <summary>
    /// Determines whether two entities are not equal.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns><see langword="true"/> if the entities are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}