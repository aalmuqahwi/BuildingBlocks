using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Non-generic-command entry point used to dispatch to a strongly-typed command handler.
/// </summary>
/// <typeparam name="TResult">The type of result produced by handling the command.</typeparam>
public interface ICommandHandlerWrapper<TResult> where TResult : Result
{
    /// <summary>
    /// Handles the specified command.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The result of handling the command.</returns>
    Task<TResult> Handle(ICommand<TResult> command, CancellationToken cancellationToken);
}