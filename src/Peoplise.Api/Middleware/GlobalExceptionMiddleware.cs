using Microsoft.AspNetCore.Mvc;

namespace Peoplise.Api.Middleware;

/// <summary>
/// Catches any exception that escapes a controller/endpoint — i.e. a bug, not an
/// expected failure (those are handled inline via <c>Result</c>/<c>Error</c> and turned
/// into the right status code by the controller itself) — logs it, and returns a
/// uniform <see cref="ProblemDetails"/> response instead of an ASP.NET Core default
/// error page or a stack trace leaking to the client.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = _environment.IsDevelopment() ? ex.ToString() : null,
                Instance = context.Request.Path,
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
