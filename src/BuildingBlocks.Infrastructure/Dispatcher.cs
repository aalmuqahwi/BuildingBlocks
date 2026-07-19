using BuildingBlocks.Application;
using BuildingBlocks.Domain;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Default <see cref="IDispatcher"/> implementation that resolves handlers from an <see cref="IServiceProvider"/>.
/// </summary>
public sealed class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Dispatcher"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve command and query handlers.</param>
    public Dispatcher(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public Task<TResult> QueryAsync<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default) where TResult : Result
    {
        var wrapper = _serviceProvider
            .GetRequiredKeyedService<IQueryHandlerWrapper<TResult>>(query.GetType());

        return RunPipeline(
            query,
            () => wrapper.Handle(query, cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<TResult> SendAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default) where TResult : Result
    {
        var wrapper = _serviceProvider
            .GetRequiredKeyedService<ICommandHandlerWrapper<TResult>>(command.GetType());

        return RunPipeline(
            command,
            () => wrapper.Handle(command, cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result> SendAsync(
        ICommand command,
        CancellationToken cancellationToken = default) => SendAsync<Result>(command, cancellationToken);

    private Task<TResult> RunPipeline<TRequest, TResult>(
        TRequest request,
        RequestHandlerDelegate<TResult> handler,
        CancellationToken cancellationToken)
        where TResult : Result
    {
        var behaviors = _serviceProvider.GetServices<IPipelineBehavior<TRequest, TResult>>();

        var next = handler;

        foreach (var behavior in behaviors.Reverse())
        {
            var current = next;
            next = () => behavior.Handle(request, current, cancellationToken);
        }

        return next();
    }
}