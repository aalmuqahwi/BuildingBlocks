namespace BuildingBlocks.Application;

/// <summary>
/// Invokes the next behavior or handler in a request processing pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of response produced.</typeparam>
/// <returns>The response produced by the next step in the pipeline.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();