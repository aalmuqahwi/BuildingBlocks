# BuildingBlocks

Reusable Domain/Application/Infrastructure building blocks — DDD, Clean Architecture, CQRS, and Modular Monolith patterns — for .NET.

## Status
🚧 Actively developed. Built to learn NuGet packaging and reduce copy-paste across my own projects — APIs may still change between versions.

## Installation

```
dotnet add package aalmuqahwi.BuildingBlocks.Domain
dotnet add package aalmuqahwi.BuildingBlocks.Application
dotnet add package aalmuqahwi.BuildingBlocks.Infrastructure
```

## Packages

### `BuildingBlocks.Domain`
Core DDD primitives. No external dependencies.
- `Result` / `Error` — success/failure without exceptions
- `Entity<TId>` / `AggregateRoot<TId>` — equality by ID, domain event tracking
- `DomainEvent` / `IDomainEvent` — immutable facts raised by aggregates
- `ValueObject` — base for immutable, structurally-equal types

### `BuildingBlocks.Application`
CQRS and cross-cutting contracts. Depends only on `BuildingBlocks.Domain`.
- `ICommand` / `IQuery<TResult>` — request markers
- `ICommandHandler<,>` / `IQueryHandler<,>` — handler contracts
- `IDispatcher` — routes commands/queries to their handlers
- `IPipelineBehavior<,>` — cross-cutting middleware around handler execution (logging, validation, etc.)
- `IUnitOfWork` — persistence flush abstraction, technology-agnostic
- `IDomainEventHandler<>` / `IDomainEventDispatcher` — domain event handling contracts
- `IIntegrationEvent` / `IIntegrationEventHandler<>` / `IIntegrationEventDispatcher` — cross-boundary event contracts (module-to-module or service-to-service)
- `IOutboxWriter` — in-memory buffer for integration events raised during a unit of work
- `OutboxMessage` — persisted outbox record (`Type`, `Payload`, `ProcessedOnUtc`, `Error`, `RetryCount`)
- `IOutboxMessageStore` / `IOutboxProcessor` — outbox persistence and processing contracts, technology-agnostic

### `BuildingBlocks.Infrastructure`
Concrete DI-based implementations. Depends on `BuildingBlocks.Application`.
- `Dispatcher` — resolves and invokes command/query handlers via keyed DI, runs pipeline behaviors
- `DomainEventDispatcher` — resolves and invokes all registered handlers for a given domain event
- `IntegrationEventDispatcher` — resolves and invokes all registered handlers for a given integration event
- `OutboxWriter` — in-memory `IOutboxWriter` implementation
- `OutboxProcessor` — reads pending outbox messages, dispatches them, and marks each processed or failed independently (one bad message never blocks the rest of the batch)
- `AddBuildingBlocksInfrastructure(...)` — one-line registration for dispatching (or call `AddDispatcher(...)` / `AddDomainEventDispatcher(...)` / `AddOutboxProcessor()` individually)

## Usage

```csharp
// Program.cs
builder.Services.AddBuildingBlocksInfrastructure(typeof(Program).Assembly);

// Somewhere in your app
public record CreateOrderCommand(string ProductId) : ICommand<Result<Guid>>;

public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateOrderCommand command, CancellationToken ct)
        => Task.FromResult(Result.Success(Guid.NewGuid()));
}

// Dispatching
var result = await dispatcher.SendAsync(new CreateOrderCommand("prod-123"));
```

**Outbox / integration events** — raise an event during a unit of work, flush it to storage when saving, then process it independently.

Raise and persist:
```csharp
// Somewhere handling a command
_outboxWriter.Add(new OrderShipped(orderId));

// Inside your DbContext's SaveChangesAsync override (or equivalent)
foreach (var integrationEvent in _outboxWriter.Flush())
{
    OutboxMessages.Add(new OutboxMessage
    {
        Id = Guid.NewGuid(),
        Type = integrationEvent.GetType().AssemblyQualifiedName!,
        Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
        OccurredOnUtc = DateTime.UtcNow
    });
}
```

Register and process, triggered by whatever scheduler you prefer:
```csharp
// Program.cs
builder.Services.AddOutboxProcessor();
builder.Services.AddScoped<IIntegrationEventHandler<OrderShipped>, SendShippingEmail>();
builder.Services.AddScoped<IOutboxMessageStore, EfOutboxMessageStore>(); // your own implementation

// Triggered by your own BackgroundService, Hangfire, Quartz, etc.
await outboxProcessor.ProcessPendingAsync(ct);
```

## Design notes
- `Domain` has zero dependencies by design — safe to reference from anywhere
- `IUnitOfWork` and persistence are intentionally technology-agnostic — bring your own EF Core/Dapper/etc. implementation, including `IOutboxMessageStore`
- Each package only depends on what it strictly needs — `Application` and `Infrastructure` don't force unrelated dependencies onto consumers who only want a subset
- **Integration event handlers are registered manually**, not auto-scanned like command/domain event handlers — unlike those, integration events may cross service boundaries, so implicit assembly scanning could pick up a handler that was never meant to run in a given service. Forgetting to register a handler causes it to silently never run — no error, no warning.
- **Outbox delivery is at-least-once, not exactly-once.** If the process crashes between dispatching and marking a message processed, it will be redelivered on the next run. Integration event handlers should be idempotent.
- **`OutboxProcessor` reports failures and computes the next retry count, but your `IOutboxMessageStore` owns retry policy.** `MarkFailedAsync` is called with the handler's actual error message and an incremented `RetryCount` — it's up to your store implementation to decide when a message has retried too many times and stop returning it from `GetPendingAsync` (or move it to a dead-letter state). Without this, a permanently unresolvable message will be retried forever.
- **No built-in scheduler for `IOutboxProcessor`.** `ProcessPendingAsync()` is a plain, injectable method — call it from a `BackgroundService`, a Hangfire recurring job, a Quartz job, or anything else. Trigger it from exactly one place; running two triggers concurrently against the same store can cause duplicate or racing delivery.