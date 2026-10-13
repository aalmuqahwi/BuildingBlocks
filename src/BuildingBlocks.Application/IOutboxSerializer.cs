namespace BuildingBlocks.Application;

/// <summary>
/// Converts events to and from their persisted outbox representation, and names the types stored alongside them.
/// </summary>
public interface IOutboxSerializer
{
    /// <summary>
    /// Serializes the specified event.
    /// </summary>
    /// <param name="value">The event to serialize.</param>
    /// <returns>The serialized payload.</returns>
    string Serialize(object value);

    /// <summary>
    /// Deserializes the specified payload into an instance of <paramref name="type"/>.
    /// </summary>
    /// <param name="payload">The serialized payload.</param>
    /// <param name="type">The type to deserialize into.</param>
    /// <returns>The deserialized event.</returns>
    object Deserialize(string payload, Type type);

    /// <summary>
    /// Gets the name under which the specified event or handler type is persisted.
    /// </summary>
    /// <param name="type">The type to name.</param>
    /// <returns>The persisted type name.</returns>
    string GetTypeName(Type type);

    /// <summary>
    /// Resolves a type from a name produced by <see cref="GetTypeName"/>.
    /// </summary>
    /// <param name="typeName">The persisted type name.</param>
    /// <returns>The resolved type.</returns>
    Type ResolveType(string typeName);
}