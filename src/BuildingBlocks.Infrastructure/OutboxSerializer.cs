using System.Text.Json;

using BuildingBlocks.Application;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Default <see cref="IOutboxSerializer"/> implementation based on <see cref="JsonSerializer"/>.
/// </summary>
/// <remarks>
/// Type names are persisted as <c>"Namespace.Type, AssemblyName"</c>, without the assembly version,
/// so bumping an assembly version does not break messages that are still pending.
/// Renaming or moving a type does; implement <see cref="IOutboxSerializer"/> to use stable names of your own.
/// </remarks>
public sealed class OutboxSerializer : IOutboxSerializer
{
    private readonly JsonSerializerOptions? _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxSerializer"/> class.
    /// </summary>
    /// <param name="options">The JSON options used for payloads, for example to add converters for value objects.</param>
    public OutboxSerializer(JsonSerializerOptions? options = null) => _options = options;

    /// <inheritdoc />
    public string Serialize(object value)
        => JsonSerializer.Serialize(value, value.GetType(), _options);

    /// <inheritdoc />
    public object Deserialize(string payload, Type type)
        => JsonSerializer.Deserialize(payload, type, _options)
            ?? throw new InvalidOperationException($"Payload deserialized to null for type '{type}'.");

    /// <inheritdoc />
    public string GetTypeName(Type type)
        => $"{type.FullName}, {type.Assembly.GetName().Name}";

    /// <inheritdoc />
    public Type ResolveType(string typeName)
        => Type.GetType(typeName)
            ?? throw new InvalidOperationException($"Cannot resolve type '{typeName}'.");
}