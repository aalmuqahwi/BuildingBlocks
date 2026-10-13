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
    public async Task DispatchAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IIntegrationEventHandler<>)
            .MakeGenericType(integrationEvent.GetType());

        foreach (IIntegrationEventHandler handler in _serviceProvider
            .GetServices(handlerType).Cast<IIntegrationEventHandler>())
        {
            await handler.Handle(integrationEvent, cancellationToken);
        }
    }
}