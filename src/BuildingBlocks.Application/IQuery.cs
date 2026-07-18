using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Represents a query that produces a <typeparamref name="TResult"/> when handled.
/// </summary>
/// <typeparam name="TResult">The type of result produced by handling the query.</typeparam>
public interface IQuery<TResult> where TResult : Result;