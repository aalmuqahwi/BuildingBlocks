# BuildingBlocks

Reusable Domain/Application/Infrastructure building blocks — DDD, Clean Architecture, CQRS, and Modular Monolith patterns — for .NET.

## Status
🚧 Actively developed — built to learn NuGet packaging and reduce copy-paste across my own projects. APIs may still change.

## Packages

### `BuildingBlocks.Domain`
Core DDD primitives, no external dependencies.
- `Result` / `Error` — success/failure without exceptions
- `Entity<TId>` / `AggregateRoot<TId>` — equality by ID, domain event tracking
- `DomainEvent` / `IDomainEvent` — immutable facts raised by aggregates
- `ValueObject` — base for immutable, structurally-equal types

### `BuildingBlocks.Application`
CQRS and cross-cutting contracts, depends only on `BuildingBlocks.Domain`.
- `ICommand` / `IQuery<TResult>` — request markers
- `ICommandHandler<,>` / `IQueryHandler<,>` — handler contracts
- `IDispatcher` — routes commands/queries to their handlers
- `IPipelineBehavior<,>` — cross-cutting middleware around handler execution (logging, validation, etc.)
- `IUnitOfWork` — persistence flush abstraction, technology-agnostic
- `IDomainEventHandler<>` / `IDomainEventDispatcher` — domain event handling contracts

### `BuildingBlocks.Infrastructure`
Concrete DI-based implementations, depends on `BuildingBlocks.Application`.
- `Dispatcher` — resolves and invokes command/query handlers via keyed DI, runs pipeline behaviors
- `DomainEventDispatcher` — resolves and invokes all registered handlers for a given domain event
- `AddBuildingBlocksInfrastructure(...)` — one-line registration for everything above (or call `AddDispatcher(...)` / `AddDomainEventDispatcher(...)` individually)

## Installation

```
dotnet add package aalmuqahwi.BuildingBlocks.Domain
dotnet add package aalmuqahwi.BuildingBlocks.Application
dotnet add package aalmuqahwi.BuildingBlocks.Infrastructure
```

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

## Design notes
- `Domain` has zero dependencies by design — it's safe to reference from anywhere
- `IUnitOfWork` and persistence are intentionally technology-agnostic — bring your own EF Core/Dapper/etc. implementation