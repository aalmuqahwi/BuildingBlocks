using System.Reflection;

using BuildingBlocks.Application;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Extension methods for registering the dispatcher and its handlers with dependency injection.
/// </summary>
public static class DispatcherServiceExtensions
{
    /// <summary>
    /// Registers the <see cref="IDispatcher"/> and all command and query handlers found in the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="assemblies">The assemblies to scan for command and query handlers.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddDispatcher(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddScoped<IDispatcher, Dispatcher>();

        RegisterHandlers(services, assemblies, typeof(ICommandHandler<,>));
        RegisterHandlers(services, assemblies, typeof(IQueryHandler<,>));

        RegisterWrappers(
            services, assemblies,
            typeof(ICommandHandler<,>),
            typeof(ICommandHandlerWrapper<>));

        RegisterWrappers(
            services, assemblies,
            typeof(IQueryHandler<,>),
            typeof(IQueryHandlerWrapper<>));

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

    private static void RegisterWrappers(
        IServiceCollection services,
        Assembly[] assemblies,
        Type handlerInterface,
        Type wrapperInterface)
    {
        var registrations = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is
            {
                IsAbstract: false, IsInterface: false,
                IsGenericTypeDefinition: false
            })
            .SelectMany(concreteType => concreteType
                .GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == handlerInterface)
                .Select(i => new
                {
                    ConcreteType = concreteType,
                    TFirst = i.GetGenericArguments()[0],
                    TResult = i.GetGenericArguments()[1]
                }));

        foreach (var reg in registrations)
        {
            var wrapper = wrapperInterface.MakeGenericType(reg.TResult);

            // Key = command/query type — unique per handler, avoids collision
            services.AddKeyedScoped(wrapper, reg.TFirst, reg.ConcreteType);
        }
    }
}