using BuildingBlocks.Application;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Configures the outbox services registered by <see cref="OutboxServiceExtensions.AddOutboxProcessor"/>.
/// </summary>
public sealed class OutboxOptions
{
    internal ServiceDescriptor? Serializer { get; private set; }

    /// <summary>
    /// Uses <typeparamref name="TSerializer"/>, resolved from dependency injection as a singleton, instead of the default <see cref="OutboxSerializer"/>.
    /// </summary>
    /// <typeparam name="TSerializer">The serializer type.</typeparam>
    /// <returns>The options, for chaining.</returns>
    public OutboxOptions UseSerializer<TSerializer>() where TSerializer : class, IOutboxSerializer
    {
        Serializer = ServiceDescriptor.Singleton<IOutboxSerializer, TSerializer>();
        return this;
    }

    /// <summary>
    /// Uses the specified serializer instead of the default <see cref="OutboxSerializer"/>.
    /// </summary>
    /// <remarks>
    /// To keep the default serializer with custom JSON settings, pass <c>new OutboxSerializer(jsonOptions)</c>.
    /// </remarks>
    /// <param name="serializer">The serializer to use.</param>
    /// <returns>The options, for chaining.</returns>
    public OutboxOptions UseSerializer(IOutboxSerializer serializer)
    {
        Serializer = ServiceDescriptor.Singleton(serializer);
        return this;
    }
}