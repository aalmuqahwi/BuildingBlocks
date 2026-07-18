using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Handles queries of type <typeparamref name="TQuery"/> and produces a <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TQuery">The type of query handled.</typeparam>
/// <typeparam name="TResult">The type of result produced by handling the query.</typeparam>
public interface IQueryHandler<TQuery, TResult> : IQueryHandlerWrapper<TResult>
    where TQuery : IQuery<TResult>
    where TResult : Result
{
    /// <summary>
    /// Handles the specified query.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The result of handling the query.</returns>
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken);

    /// <inheritdoc />
    Task<TResult> IQueryHandlerWrapper<TResult>.Handle(IQuery<TResult> query, CancellationToken cancellationToken)
        => Handle((TQuery)query, cancellationToken);
}