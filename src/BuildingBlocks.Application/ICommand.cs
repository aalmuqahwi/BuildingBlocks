using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Represents a command that produces a <typeparamref name="TResult"/> when handled.
/// </summary>
/// <typeparam name="TResult">The type of result produced by handling the command.</typeparam>
public interface ICommand<TResult> where TResult : Result;

/// <summary>
/// Represents a command that produces a <see cref="Result"/> when handled.
/// </summary>
public interface ICommand : ICommand<Result>;