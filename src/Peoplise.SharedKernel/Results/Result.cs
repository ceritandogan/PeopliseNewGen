namespace Peoplise.SharedKernel.Results;

/// <summary>
/// The outcome of an operation that can fail for expected reasons: either success, or
/// failure carrying an <see cref="Results.Error"/>. Application command/query handlers
/// return this instead of throwing, so callers handle expected failures (validation,
/// not-found, conflicting rules) as data rather than via exception handling
/// (Railway-Oriented Programming).
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A successful result cannot carry an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failed result must carry an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

/// <summary>
/// A <see cref="Result"/> that additionally carries the produced <typeparamref name="TValue"/>
/// on success. Reading <see cref="Value"/> on a failed result throws, so callers must
/// check <see cref="Result.IsSuccess"/> first.
/// </summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>The successful value. Throws if this result is a failure.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
