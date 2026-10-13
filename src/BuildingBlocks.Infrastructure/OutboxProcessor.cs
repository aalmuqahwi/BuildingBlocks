using BuildingBlocks.Application;
using BuildingBlocks.Domain;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Default <see cref="IOutboxProcessor"/> implementation that delivers each pending outbox message to its handler
/// in a dedicated DI scope and transaction.
/// </summary>
/// <remarks>
/// For each message, the handler's work, any events it wrote through <see cref="IOutboxWriter"/>, and the processed mark
/// are committed together by <see cref="IUnitOfWork.SaveChangesAsync"/>. On failure, the scope is discarded and the failure
/// is recorded in a fresh scope, so partial work is never committed and never leaks into the next message.
/// </remarks>
public sealed class OutboxProcessor : IOutboxProcessor
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProcessor"/> class.
    /// </summary>
    /// <param name="scopeFactory">The factory used to create a scope per message.</param>
    public OutboxProcessor(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    /// <inheritdoc />
    public async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<OutboxMessage> pending;
        using (var scope = _scopeFactory.CreateScope())
        {
            pending = await scope.ServiceProvider
                .GetRequiredService<IOutboxMessageStore>()
                .GetPendingAsync(cancellationToken);
        }

        foreach (var message in pending)
        {
            try
            {
                await ProcessAsync(message, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                await MarkFailedAsync(message, ex.Message, cancellationToken);
            }
        }
    }

    private async Task ProcessAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var serializer = services.GetRequiredService<IOutboxSerializer>();
        var eventType = serializer.ResolveType(message.Type);
        var handlerType = serializer.ResolveType(message.Handler);
        var @event = serializer.Deserialize(message.Payload, eventType);

        await (@event switch
        {
            IDomainEvent domainEvent => ResolveHandler<IDomainEventHandler>(
                    services, typeof(IDomainEventHandler<>), eventType, handlerType)
                .Handle(domainEvent, cancellationToken),
            IIntegrationEvent integrationEvent => ResolveHandler<IIntegrationEventHandler>(
                    services, typeof(IIntegrationEventHandler<>), eventType, handlerType)
                .Handle(integrationEvent, cancellationToken),
            _ => throw new InvalidOperationException(
                $"Type '{message.Type}' is neither a domain event nor an integration event.")
        });

        var store = services.GetRequiredService<IOutboxMessageStore>();
        var written = services.GetRequiredService<IOutboxWriter>().Flush();
        if (written.Count > 0)
        {
            await store.AddAsync(written, cancellationToken);
        }

        await store.MarkProcessedAsync(message.Id, cancellationToken);
        await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
    }

    private async Task MarkFailedAsync(OutboxMessage message, string error, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        await services.GetRequiredService<IOutboxMessageStore>()
            .MarkFailedAsync(message.Id, error, message.RetryCount + 1, cancellationToken);
        await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
    }

    private static THandler ResolveHandler<THandler>(
        IServiceProvider services,
        Type openHandlerInterface,
        Type eventType,
        Type handlerType)
    {
        var handler = services
            .GetServices(openHandlerInterface.MakeGenericType(eventType))
            .FirstOrDefault(h => h?.GetType() == handlerType);

        return handler is THandler typed
            ? typed
            : throw new InvalidOperationException(
                $"Handler '{handlerType}' is not registered for event '{eventType}'.");
    }
}