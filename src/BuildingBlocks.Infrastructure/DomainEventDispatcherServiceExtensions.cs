using System.Reflection;

using BuildingBlocks.Application;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Extension methods for registering the domain event dispatcher and its handlers with dependency injection.
/// </summary>
public static class DomainEventDispatcherServiceExtensions
{
    /// <summary>
    /// Registers the <see cref="IDomainEventDispatcher"/> and all domain event handlers found in the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="assemblies">The assemblies to scan for domain event handlers.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddDomainEventDispatcher(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        RegisterHandlers(services, assemblies, typeof(IDomainEventHandler<>));

        return services;
    }

    private static void RegisterHandlers(
        IServiceCollection services,
        Assembly[] assemblies,
        Type handlerInterface)
    {
        var registrations = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false })
            .SelectMany(concreteType => concreteType
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                .Select(i => new { Interface = i, ConcreteType = concreteType }));

        foreach (var reg in registrations)
        {
            services.AddScoped(reg.Interface, reg.ConcreteType);
        }
    }
}