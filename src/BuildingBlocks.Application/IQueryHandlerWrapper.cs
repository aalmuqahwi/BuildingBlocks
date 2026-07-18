using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Non-generic-query entry point used to dispatch to a strongly-typed query handler.
/// </summary>
/// <typeparam name="TResult">The type of result produced by handling the query.</typeparam>
public interface IQueryHandlerWrapper<TResult> where TResult : Result
{
    /// <summary>
    /// Handles the specified query.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The result of handling the query.</returns>
    Task<TResult> Handle(IQuery<TResult> query, CancellationToken cancellationToken);
}