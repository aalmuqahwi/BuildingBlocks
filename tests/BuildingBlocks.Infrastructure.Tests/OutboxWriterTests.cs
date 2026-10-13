using BuildingBlocks.Application;
using BuildingBlocks.Domain;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Tests;

#pragma warning disable CS1591
public class OutboxWriterTests
{
    public class EventWithoutHandlers : DomainEvent;

    public class HandlerOne : IDomainEventHandler<OrderPlaced>
    {
        public Task Handle(OrderPlaced domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class HandlerTwo : IDomainEventHandler<OrderPlaced>
    {
        public Task Handle(OrderPlaced domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class FactoryRegisteredHandler : IIntegrationEventHandler<OrderPlacedIntegrationEvent>
    {
        public Task Handle(OrderPlacedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private static IOutboxWriter CreateWriter(Action<IServiceCollection> registerHandlers)
    {
        var services = new ServiceCollection();
        services.AddOutboxProcessor();
        registerHandlers(services);

        return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IOutboxWriter>();
    }

    [Fact]
    public void Flush_should_produce_one_message_per_registered_handler()
    {
        var writer = CreateWriter(s =>
        {
            s.AddScoped<IDomainEventHandler<OrderPlaced>, HandlerOne>();
            s.AddScoped<IDomainEventHandler<OrderPlaced>, HandlerTwo>();
        });
        var domainEvent = new OrderPlaced { OrderId = Guid.NewGuid() };

        writer.Add(domainEvent);
        var messages = writer.Flush();

        Assert.Equal(2, messages.Count);
        Assert.Equal(2, messages.Select(m => m.Id).Distinct().Count());
        Assert.Equal(2, messages.Select(m => m.Handler).Distinct().Count());
        Assert.All(messages, m => Assert.Equal(domainEvent.OccurredOnUtc, m.OccurredOnUtc));
        Assert.Single(messages.Select(m => m.Payload).Distinct());
    }

    [Fact]
    public void Flush_should_produce_no_messages_for_event_without_handlers()
    {
        var writer = CreateWriter(_ => { });

        writer.Add(new EventWithoutHandlers());

        Assert.Empty(writer.Flush());
    }

    [Fact]
    public void Flush_should_clear_the_buffer()
    {
        var writer = CreateWriter(s => s.AddScoped<IDomainEventHandler<OrderPlaced>, HandlerOne>());
        writer.Add(new OrderPlaced());

        writer.Flush();

        Assert.Empty(writer.Flush());
    }

    [Fact]
    public void Flush_should_find_handlers_registered_after_AddOutboxProcessor()
    {
        var services = new ServiceCollection();
        services.AddOutboxProcessor();
        services.AddScoped<IDomainEventHandler<OrderPlaced>, HandlerOne>();
        var writer = services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IOutboxWriter>();

        writer.Add(new OrderPlaced());

        Assert.Single(writer.Flush());
    }

    [Fact]
    public void Flush_should_throw_when_handler_is_registered_with_a_factory()
    {
        var writer = CreateWriter(s =>
            s.AddScoped<IIntegrationEventHandler<OrderPlacedIntegrationEvent>>(_ => new FactoryRegisteredHandler()));

        writer.Add(new OrderPlacedIntegrationEvent());

        Assert.Throws<InvalidOperationException>(() => writer.Flush());
    }
}

public class OutboxSerializerTests
{
    private readonly OutboxSerializer _serializer = new();

    [Fact]
    public void Domain_event_should_round_trip_with_its_id_and_occurrence_time()
    {
        var original = new OrderPlaced
        {
            OrderId = Guid.NewGuid(),
            OccurredOnUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)
        };

        var payload = _serializer.Serialize(original);
        var restored = (OrderPlaced)_serializer.Deserialize(payload, typeof(OrderPlaced));

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.OccurredOnUtc, restored.OccurredOnUtc);
        Assert.Equal(original.OrderId, restored.OrderId);
    }

    [Fact]
    public void GetTypeName_should_omit_assembly_version_and_round_trip()
    {
        var name = _serializer.GetTypeName(typeof(OrderPlaced));

        Assert.DoesNotContain("Version=", name);
        Assert.Equal(typeof(OrderPlaced), _serializer.ResolveType(name));
    }

    [Fact]
    public void ResolveType_should_throw_for_unknown_type()
        => Assert.Throws<InvalidOperationException>(() => _serializer.ResolveType("Unknown.Type, Unknown.Assembly"));
}
#pragma warning restore CS1591