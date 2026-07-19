using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Handles commands of type <typeparamref name="TCommand"/> and produces a <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TCommand">The type of command handled.</typeparam>
/// <typeparam name="TResult">The type of result produced by handling the command.</typeparam>
public interface ICommandHandler<TCommand, TResult> : ICommandHandlerWrapper<TResult>
    where TCommand : ICommand<TResult>
    where TResult: Result
{
    /// <summary>
    /// Handles the specified command.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The result of handling the command.</returns>
    Task<TResult> Handle(TCommand command, CancellationToken cancellationToken);

    /// <inheritdoc />
    Task<TResult> ICommandHandlerWrapper<TResult>.Handle(ICommand<TResult> command, CancellationToken cancellationToken)
        => Handle((TCommand)command, cancellationToken);
}

/// <summary>
/// Handles commands of type <typeparamref name="TCommand"/> that produce a plain <see cref="Result"/>.
/// </summary>
/// <typeparam name="TCommand">The type of command handled.</typeparam>
public interface ICommandHandler<TCommand> : ICommandHandler<TCommand, Result>
    where TCommand : ICommand { }