using BuildingBlocks.Application;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Extension methods for registering outbox processing services with dependency injection.
/// </summary>
public static class OutboxServiceExtensions
{
    /// <summary>
    /// Registers the outbox writer, integration event dispatcher, and outbox processor.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddOutboxProcessor(this IServiceCollection services)
    {
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IIntegrationEventDispatcher, IntegrationEventDispatcher>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();

        return services;
    }
}