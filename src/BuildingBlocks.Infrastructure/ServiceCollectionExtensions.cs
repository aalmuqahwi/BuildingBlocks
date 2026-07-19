using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Extension methods for registering BuildingBlocks infrastructure services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the BuildingBlocks infrastructure services, including the dispatcher and its handlers.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="assemblies">The assemblies to scan for command and query handlers.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddBuildingBlocksInfrastructure(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddDispatcher(assemblies);

        return services;
    }
}