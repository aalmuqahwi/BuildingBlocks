using BuildingBlocks.Application;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Tests;

#pragma warning disable CS1591
public class OutboxOptionsTests
{
    public class CustomSerializer : IOutboxSerializer
    {
        public string Serialize(object value) => throw new NotImplementedException();
        public object Deserialize(string payload, Type type) => throw new NotImplementedException();
        public string GetTypeName(Type type) => throw new NotImplementedException();
        public Type ResolveType(string typeName) => throw new NotImplementedException();
    }

    private static IOutboxSerializer ResolveSerializer(IServiceCollection services)
        => services.BuildServiceProvider().GetRequiredService<IOutboxSerializer>();

    [Fact]
    public void AddOutboxProcessor_should_use_the_default_serializer_when_not_configured()
    {
        var services = new ServiceCollection();
        services.AddOutboxProcessor();

        Assert.IsType<OutboxSerializer>(ResolveSerializer(services));
    }

    [Fact]
    public void UseSerializer_should_register_the_serializer_type()
    {
        var services = new ServiceCollection();
        services.AddOutboxProcessor(config => config.UseSerializer<CustomSerializer>());

        Assert.IsType<CustomSerializer>(ResolveSerializer(services));
    }

    [Fact]
    public void UseSerializer_should_register_the_serializer_instance()
    {
        var serializer = new OutboxSerializer();
        var services = new ServiceCollection();
        services.AddOutboxProcessor(config => config.UseSerializer(serializer));

        Assert.Same(serializer, ResolveSerializer(services));
    }

    [Fact]
    public void UseSerializer_should_win_over_a_serializer_registered_earlier()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOutboxSerializer>(new OutboxSerializer());
        services.AddOutboxProcessor(config => config.UseSerializer<CustomSerializer>());

        Assert.IsType<CustomSerializer>(ResolveSerializer(services));
        Assert.Single(services, d => d.ServiceType == typeof(IOutboxSerializer));
    }

    [Fact]
    public void AddBuildingBlocksInfrastructure_should_pass_outbox_configuration_through()
    {
        var services = new ServiceCollection();
        services.AddBuildingBlocksInfrastructure(
            config => config.UseSerializer<CustomSerializer>(),
            typeof(OutboxOptionsTests).Assembly);

        Assert.IsType<CustomSerializer>(ResolveSerializer(services));
    }
}
#pragma warning restore CS1591