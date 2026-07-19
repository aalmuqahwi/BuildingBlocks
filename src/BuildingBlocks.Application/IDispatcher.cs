using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Dispatches commands and queries to their registered handlers.
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// Sends a command to its registered handler.
    /// </summary>
    /// <typeparam name="TResult">The type of result produced by handling the command.</typeparam>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The result of handling the command.</returns>
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
        where TResult : Result;

    /// <summary>
    /// Sends a command to its registered handler.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The result of handling the command.</returns>
    Task<Result> SendAsync(ICommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a query to its registered handler.
    /// </summary>
    /// <typeparam name="TResult">The type of result produced by handling the query.</typeparam>
    /// <param name="query">The query to send.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The result of handling the query.</returns>
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        where TResult : Result;
}