namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for aggregate roots identified by an <typeparamref name="TId"/> value.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root's identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot{TId}"/> class.
    /// </summary>
    /// <param name="id">The aggregate root's identifier.</param>
    protected AggregateRoot(TId id) : base(id) {}
}