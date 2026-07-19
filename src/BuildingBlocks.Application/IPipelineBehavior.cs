namespace BuildingBlocks.Application;

/// <summary>
/// Represents a cross-cutting behavior that runs before and/or after a request is handled.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response produced by handling the request.</typeparam>
public interface IPipelineBehavior<TRequest, TResponse>
{
    /// <summary>
    /// Handles the specified request, optionally invoking the next behavior or handler in the pipeline.
    /// </summary>
    /// <param name="request">The request being handled.</param>
    /// <param name="next">A delegate that invokes the next behavior or handler in the pipeline.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The response produced by the pipeline.</returns>
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}