using BuildingBlocks.Domain;
using BuildingBlocks.Application;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Default <see cref="IDomainEventDispatcher"/> implementation that resolves handlers from an <see cref="IServiceProvider"/>.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventDispatcher"/> class.
    /// </summary>
    /// <param name="sp">The service provider used to resolve domain event handlers.</param>
    public DomainEventDispatcher(IServiceProvider sp) => _serviceProvider = sp;

    /// <inheritdoc />
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IDomainEventHandler<>)
            .MakeGenericType(domainEvent.GetType());

        foreach (IDomainEventHandler handler in _serviceProvider
            .GetServices(handlerType).Cast<IDomainEventHandler>())
        {
            await handler.Handle(domainEvent, cancellationToken);
        }
    }
}