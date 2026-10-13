using BuildingBlocks.Application;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Extension methods for registering outbox processing services with dependency injection.
/// </summary>
public static class OutboxServiceExtensions
{
    /// <summary>
    /// Registers the outbox writer, serializer, integration event dispatcher, and outbox processor.
    /// </summary>
    /// <remarks>
    /// You still need to register your own <see cref="IOutboxMessageStore"/> and <see cref="IUnitOfWork"/>.
    /// </remarks>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configure">Configures the outbox, for example to replace the default <see cref="OutboxSerializer"/>.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddOutboxProcessor(
        this IServiceCollection services,
        Action<OutboxOptions>? configure = null)
    {
        var options = new OutboxOptions();
        configure?.Invoke(options);

        // The registry reads the final registrations lazily, so handlers registered after this call are still found
        services.TryAddSingleton(_ => new OutboxHandlerRegistry(services));

        if (options.Serializer is not null)
        {
            services.Replace(options.Serializer);
        }
        else
        {
            services.TryAddSingleton<IOutboxSerializer>(_ => new OutboxSerializer());
        }

        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IIntegrationEventDispatcher, IntegrationEventDispatcher>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();

        return services;
    }
}