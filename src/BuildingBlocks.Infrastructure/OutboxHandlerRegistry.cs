using System.Collections.Concurrent;

using BuildingBlocks.Application;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Maps event types to the concrete handler types registered for them, read from the service registrations.
/// </summary>
/// <remarks>
/// Reading the registrations rather than resolving handlers keeps integration event handlers manually registered
/// and avoids constructing handlers just to learn their types.
/// </remarks>
internal sealed class OutboxHandlerRegistry
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(IDomainEventHandler<>),
        typeof(IIntegrationEventHandler<>)
    ];

    private readonly IServiceCollection _services;
    private readonly ConcurrentDictionary<Type, IReadOnlyList<Type>> _cache = new();

    public OutboxHandlerRegistry(IServiceCollection services) => _services = services;

    public IReadOnlyList<Type> GetHandlerTypes(Type eventType)
        => _cache.GetOrAdd(eventType, FindHandlerTypes);

    private IReadOnlyList<Type> FindHandlerTypes(Type eventType)
    {
        return _services
            .Where(d => !d.IsKeyedService
                && d.ServiceType.IsConstructedGenericType
                && HandlerInterfaces.Contains(d.ServiceType.GetGenericTypeDefinition())
                && d.ServiceType.GenericTypeArguments[0] == eventType)
            .Select(d => d.ImplementationType
                ?? d.ImplementationInstance?.GetType()
                ?? throw new InvalidOperationException(
                    $"Handler for '{eventType}' is registered with a factory, so its type cannot be determined. " +
                    "Register outbox handlers by type or instance."))
            .Distinct()
            .ToList();
    }
}