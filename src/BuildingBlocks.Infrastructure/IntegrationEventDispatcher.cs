using BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Default <see cref="IIntegrationEventDispatcher"/> implementation that resolves handlers from an <see cref="IServiceProvider"/>.
/// </summary>
public sealed class IntegrationEventDispatcher : IIntegrationEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationEventDispatcher"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve integration event handlers.</param>
    public IntegrationEventDispatcher(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public async Task DispatchAsync<T>(T integrationEvent, CancellationToken cancellationToken)
        where T : IIntegrationEvent
    {
        var handlers = _serviceProvider.GetServices<IIntegrationEventHandler<T>>();

        foreach (var handler in handlers)
        {
            await handler.Handle(integrationEvent, cancellationToken);
        }
    }
}