namespace BuildingBlocks.Domain;

/// <summary>
/// Represents the outcome of an operation, indicating success or failure with an associated <see cref="Domain.Error"/>.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">The error associated with the result. Must be <see cref="Domain.Error.None"/> for a successful result and non-<see cref="Domain.Error.None"/> for a failed result.</param>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A successful result cannot carry an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failed result must carry an error.");
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error associated with the result, or <see cref="Domain.Error.None"/> if the result succeeded.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(Error error) => new(false, error);
}

/// <summary>
/// Represents the outcome of an operation that produces a value of type <typeparamref name="T"/> on success.
/// </summary>
/// <typeparam name="T">The type of the value produced on success.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="value">The value produced by a successful operation.</param>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">The error associated with the result.</param>
    internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
        => _value = value;

    /// <summary>
    /// Gets the value produced by a successful operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    /// <summary>
    /// Creates a successful result carrying the specified value.
    /// </summary>
    /// <param name="value">The value produced by the successful operation.</param>
    /// <returns>A successful <see cref="Result{T}"/>.</returns>
    public static Result<T> Success(T value) => new(value, true, Error.None);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public static new Result<T> Failure(Error error) => new(default, false, error);
}