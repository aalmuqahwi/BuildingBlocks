namespace BuildingBlocks.Application;

/// <summary>
/// Coordinates the persistence of changes made within a unit of work.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all changes made within the unit of work.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}