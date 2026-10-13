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
        => services.AddBuildingBlocksInfrastructure(_ => { }, assemblies);

    /// <summary>
    /// Registers the BuildingBlocks infrastructure services, including the dispatcher and its handlers, with outbox configuration.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configureOutbox">Configures the outbox, for example to replace the default <see cref="OutboxSerializer"/>.</param>
    /// <param name="assemblies">The assemblies to scan for command, query, and domain event handlers.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddBuildingBlocksInfrastructure(
        this IServiceCollection services,
        Action<OutboxOptions> configureOutbox,
        params Assembly[] assemblies)
    {
        services.AddDispatcher(assemblies);
        services.AddDomainEventDispatcher(assemblies);
        services.AddOutboxProcessor(configureOutbox);

        return services;
    }
}