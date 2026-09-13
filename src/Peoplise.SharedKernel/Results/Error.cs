namespace Peoplise.SharedKernel.Results;

/// <summary>
/// A named, categorized failure reason. Errors are values, not exceptions — they
/// describe an expected failure mode (validation, a missing record, a conflicting
/// business rule) that the caller is expected to branch on, not a bug.
/// </summary>
/// <param name="Code">A short, stable, machine-readable identifier, e.g. "Position.NotFound".</param>
/// <param name="Message">A human-readable description suitable for logs or API responses.</param>
/// <param name="Type">The category of failure, used by API layers to pick an HTTP status code.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}

/// <summary>
/// The category of an <see cref="Error"/>, used to translate a failed <see cref="Result"/>
/// into the right HTTP status code at the API boundary.
/// </summary>
public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict,
}
