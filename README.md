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
- `DomainEvent` / `IDomainEvent` — immutable facts raised by aggregates, each with an `Id` and `OccurredOnUtc`
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
- `IOutboxWriter` — in-memory buffer for domain and integration events raised during a unit of work
- `OutboxMessage` — persisted outbox record, one per event per handler (`Type`, `Handler`, `Payload`, `ProcessedOnUtc`, `Error`, `RetryCount`)
- `IOutboxSerializer` — converts events to and from payloads and names the persisted types
- `IOutboxMessageStore` / `IOutboxProcessor` — outbox persistence and processing contracts, technology-agnostic

### `BuildingBlocks.Infrastructure`
Concrete DI-based implementations. Depends on `BuildingBlocks.Application`.
- `Dispatcher` — resolves and invokes command/query handlers via keyed DI, runs pipeline behaviors
- `DomainEventDispatcher` — resolves and invokes all registered handlers for a given domain event
- `IntegrationEventDispatcher` — resolves and invokes all registered handlers for a given integration event
- `OutboxWriter` — in-memory `IOutboxWriter` implementation that fans each event out to one message per registered handler
- `OutboxSerializer` — default `System.Text.Json` serializer, with version-independent type names
- `OutboxOptions` — outbox configuration passed to `AddOutboxProcessor(config => ...)` / `AddBuildingBlocksInfrastructure(config => ..., assemblies)`, e.g. `UseSerializer<T>()`
- `OutboxProcessor` — delivers each pending message to its handler in its own DI scope and transaction, and marks it processed or failed independently (one bad message never blocks the rest of the batch)
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

### Outbox: domain and integration events

**Why everything goes through the outbox.** Handling events and saving data are two separate operations, and neither order is safe:
- *Dispatch, then save* — a failing handler blocks a valid save, or an email goes out for an order whose save then fails.
- *Save, then dispatch* — the data is saved, then a handler fails or the process crashes, and the side effect is lost for good.

So events are never handled in `SaveChanges`. They are **saved** to the outbox in the same transaction as the data that raised them, and handled afterwards by `IOutboxProcessor`, with retries.

Each event is stored as **one message per registered handler**, so handlers succeed, fail and retry independently: a failing handler never causes another handler's email to be sent twice.

**1. Create the outbox table.** The library is technology-agnostic, so it ships no migrations or scripts — create one table matching `OutboxMessage` in your own database (e.g. map it in your `DbContext` and add a migration):

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | GUID | No | Primary key |
| `Type` | string, unbounded | No | Event type name, e.g. `MyApp.Orders.OrderPlaced, MyApp` |
| `Handler` | string, unbounded | No | Handler type name the message is delivered to |
| `Payload` | string, unbounded | No | Serialized event (JSON by default) |
| `OccurredOnUtc` | date/time | No | When the event occurred |
| `ProcessedOnUtc` | date/time | Yes | `NULL` while pending |
| `Error` | string, unbounded | Yes | Error from the most recent failed attempt |
| `RetryCount` | int | No | Defaults to `0` |

Keep the string columns unbounded (e.g. `nvarchar(max)` on SQL Server, `text` on PostgreSQL). Type names can be long — generic events include their type arguments' full names — and none of these columns is indexed, so a length limit gains nothing. If you do cap them, a name that doesn't fit fails the **whole** `SaveChanges`, including the business data that raised the event.

Indexes:
- **Polling (required).** The processor queries pending messages, oldest first, on every run; without an index each poll scans the whole table, which only grows.
  - With filtered indexes (SQL Server, PostgreSQL): index `(OccurredOnUtc)` `WHERE ProcessedOnUtc IS NULL` — it holds only pending rows, so it stays small.
  - Without (e.g. MySQL): index `(ProcessedOnUtc, OccurredOnUtc)`.
  - If your `GetPendingAsync` also filters on `RetryCount` (e.g. to skip dead-lettered messages), add `RetryCount` to this index.
- **Primary key on `Id`.** Covers `MarkProcessedAsync` / `MarkFailedAsync`. On SQL Server, declare it `NONCLUSTERED`: message IDs are random GUIDs, and a clustered index on random keys fragments an insert-heavy table. PostgreSQL doesn't cluster tables, so it's unaffected.
- **Cleanup (optional).** The filtered polling index excludes processed rows, so deleting old processed messages scans the table. That's fine for an occasional, batched cleanup job; if cleanup runs often or the table is very large, add an index on `ProcessedOnUtc`.

Processed messages are never deleted by the library; clean them up periodically (e.g. delete rows processed more than a week ago) to keep the table small.

**2. Persist events with your data.** Inside your `DbContext`'s `SaveChangesAsync` override (or equivalent):
```csharp
var domainEvents = ChangeTracker.Entries<IHasDomainEvents>()
    .SelectMany(e => e.Entity.PopDomainEvents());

foreach (var domainEvent in domainEvents)
    _outboxWriter.Add(domainEvent);

// Already serialized, one message per handler
OutboxMessages.AddRange(_outboxWriter.Flush());

return await base.SaveChangesAsync(ct);
```

**3. Chain events from handlers.** A domain event handler that needs to publish an integration event writes it to the outbox rather than calling a broker directly — calling the broker would reintroduce the same consistency problem:
```csharp
public class OrderPlacedHandler(IOutboxWriter outboxWriter) : IDomainEventHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced domainEvent, CancellationToken ct)
    {
        outboxWriter.Add(new OrderPlacedIntegrationEvent(domainEvent.OrderId));
        return Task.CompletedTask;
    }
}
```
The processor commits the events a handler wrote and the processed mark in the same transaction, so they are saved together or not at all.

**4. Implement the store.** Store methods only *stage* changes — the processor commits them through `IUnitOfWork`, which must share the store's scope (e.g. the same `DbContext`):
```csharp
public class EfOutboxMessageStore(AppDbContext db) : IOutboxMessageStore
{
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(CancellationToken ct)
        => await db.OutboxMessages.Where(m => m.ProcessedOnUtc == null).OrderBy(m => m.OccurredOnUtc).Take(50).ToListAsync(ct);

    public Task AddAsync(IReadOnlyList<OutboxMessage> messages, CancellationToken ct)
    {
        db.OutboxMessages.AddRange(messages);
        return Task.CompletedTask;
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken ct)
        => (await db.OutboxMessages.FindAsync([id], ct))!.ProcessedOnUtc = DateTime.UtcNow;

    public async Task MarkFailedAsync(Guid id, string error, int retryCount, CancellationToken ct)
    {
        var message = (await db.OutboxMessages.FindAsync([id], ct))!;
        message.Error = error;
        message.RetryCount = retryCount;
    }
}
```

**5. Register and process**, triggered by whatever scheduler you prefer:
```csharp
// Program.cs
builder.Services.AddBuildingBlocksInfrastructure(typeof(Program).Assembly); // scans command, query and domain event handlers
builder.Services.AddScoped<IIntegrationEventHandler<OrderPlacedIntegrationEvent>, PublishToBroker>(); // integration handlers: manual
builder.Services.AddScoped<IOutboxMessageStore, EfOutboxMessageStore>(); // your own implementation
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

// Triggered by your own BackgroundService, Hangfire, Quartz, etc.
await outboxProcessor.ProcessPendingAsync(ct);
```

To customize serialization, pass a configuration callback to `AddBuildingBlocksInfrastructure(...)` or `AddOutboxProcessor(...)`:
```csharp
// Your own serializer, resolved from DI (it can have dependencies)
builder.Services.AddBuildingBlocksInfrastructure(
    config => config.UseSerializer<MyOutboxSerializer>(),
    typeof(Program).Assembly);

// Or the default serializer with your own JSON settings (e.g. a converter)
builder.Services.AddOutboxProcessor(
    config => config.UseSerializer(new OutboxSerializer(myJsonOptions)));
```

### Upgrading from 0.2.x
- `IIntegrationEventDispatcher.DispatchAsync` is no longer generic, and `IIntegrationEvent.OccurredOn` is now `OccurredOnUtc`.
- `IDomainEvent` now requires `Id` and `OccurredOnUtc` (already provided by `DomainEvent`).
- `OutboxMessage` has a new `Handler` column, and `IOutboxMessageStore` has a new `AddAsync`; store methods must now stage changes rather than commit them, and an `IUnitOfWork` must be registered.
- **Drain the outbox before upgrading** — messages written by 0.2.x have no `Handler` and will fail to process.

## Design notes
- `Domain` has zero dependencies by design — safe to reference from anywhere
- `IUnitOfWork` and persistence are intentionally technology-agnostic — bring your own EF Core/Dapper/etc. implementation, including `IOutboxMessageStore`
- Each package only depends on what it strictly needs — `Application` and `Infrastructure` don't force unrelated dependencies onto consumers who only want a subset
- **Integration event handlers are registered manually**, not auto-scanned like command/domain event handlers — unlike those, integration events may cross service boundaries, so implicit assembly scanning could pick up a handler that was never meant to run in a given service. Forgetting to register a handler causes it to silently never run — no error, no warning.
- **Outbox handlers are found from your registrations, not by resolving them.** Domain and integration event handlers must be registered by type (`AddScoped<IDomainEventHandler<E>, H>()`) or instance; a factory-registered handler throws on flush, since its type can't be known. Handlers registered after `AddOutboxProcessor()` are still found.
- **Outbox delivery is at-least-once, not exactly-once.** A handler's database work is committed with its processed mark, but external side effects (emails, HTTP calls, broker publishes) can repeat if the process crashes before that commit. Domain event handlers now run through the outbox too, so **both domain and integration event handlers should be idempotent** — `Id` on every event is there to help.
- **Domain event handlers run after the original transaction, not inside it.** Their effects are eventually consistent. If a change must be atomic with the aggregate that raised the event, make it in the same command handler instead.
- **Events must be serializable.** Keep events to flat, primitive data (IDs, strings, numbers, dates) — not entities or aggregates. Properties need a public or `init` setter (or a matching constructor) to survive a round trip. If you need converters, pass `new OutboxSerializer(jsonOptions)` to `config.UseSerializer(...)`.
- **Persisted type names omit the assembly version**, so upgrading a package doesn't break pending messages — but **renaming or moving an event or handler type does**. Drain the outbox before such a refactor, or implement `IOutboxSerializer` with stable names of your own.
- **A handler only receives events written after it was registered.** Messages are fanned out per handler at write time; an event with no registered handlers produces no messages at all.
- **Each message is processed in its own DI scope.** A scoped `DbContext` in your store and handlers is safe: a failed message's partial work is discarded with its scope, and the failure is recorded in a fresh one, so nothing leaks into the next message.
- **`OutboxProcessor` reports failures and computes the next retry count, but your `IOutboxMessageStore` owns retry policy.** `MarkFailedAsync` is called with the handler's actual error message and an incremented `RetryCount` — it's up to your store implementation to decide when a message has retried too many times and stop returning it from `GetPendingAsync` (or move it to a dead-letter state). Without this, a permanently unresolvable message will be retried forever.
- **No built-in scheduler for `IOutboxProcessor`.** `ProcessPendingAsync()` is a plain, injectable method — call it from a `BackgroundService`, a Hangfire recurring job, a Quartz job, or anything else. Trigger it from exactly one place; running two triggers concurrently against the same store can cause duplicate or racing delivery.