using System.Reflection;
using FluentValidation;
using MediatR;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Infrastructure.Validation;

/// <summary>
/// A MediatR pipeline behavior that runs every registered <see cref="IValidator{T}"/>
/// for the incoming request before the handler runs. On failure, it short-circuits and
/// returns a <see cref="Result"/>/<see cref="Result{TValue}"/> validation failure instead
/// of calling the handler — validation failure is an expected outcome (Railway-Oriented
/// Programming), not an exception, so this never throws.
/// </summary>
/// <remarks>
/// Constrained to <c>TResponse : Result</c> specifically so every command/query in this
/// solution returns <see cref="Result"/> or <see cref="Result{TValue}"/> — both of which
/// derive from <see cref="Result"/> — which is what lets this one behavior build the
/// right failure type generically (reflecting into <see cref="Result.Failure{TValue}"/>
/// only when <typeparamref name="TResponse"/> is the generic form).
/// </remarks>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var message = string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}"));
        var error = Error.Validation("Validation.Failed", message);

        return BuildFailureResult(error);
    }

    private static TResponse BuildFailureResult(Error error)
    {
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        // TResponse is Result<TValue> — resolve and invoke the generic Failure<TValue> factory.
        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var factory = typeof(Result)
            .GetMethod(nameof(Result.Failure), 1, BindingFlags.Public | BindingFlags.Static, null, [typeof(Error)], null)!
            .MakeGenericMethod(valueType);

        return (TResponse)factory.Invoke(null, [error])!;
    }
}
