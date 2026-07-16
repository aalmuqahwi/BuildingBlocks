namespace BuildingBlocks.Domain;

/// <summary>
/// Represents an error identified by a code and a human-readable message.
/// </summary>
/// <param name="Code">A short, unique identifier for the error.</param>
/// <param name="Message">A human-readable description of the error.</param>
public sealed record Error(string Code, string Message)
{
    /// <summary>
    /// Represents the absence of an error.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);
}