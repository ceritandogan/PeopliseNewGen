using Microsoft.AspNetCore.Mvc;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Api.Controllers;

/// <summary>
/// Maps a failed <see cref="Result"/>/<see cref="Result{TValue}"/> to a
/// <c>ProblemDetails</c> response — the shape <c>@peoplise/api-client</c>'s
/// <c>ApiError</c> already assumes. A successful non-generic <see cref="Result"/> maps
/// to 204 (nothing to return); a successful <see cref="Result{TValue}"/> maps to 200
/// with the value as the body.
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess ? controller.NoContent() : ToProblem(result.Error, controller);

    public static IActionResult ToActionResult<TValue>(this Result<TValue> result, ControllerBase controller) =>
        result.IsSuccess ? controller.Ok(result.Value) : ToProblem(result.Error, controller);

    private static IActionResult ToProblem(Error error, ControllerBase controller)
    {
        var statusCode = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return controller.Problem(title: error.Code, detail: error.Message, statusCode: statusCode);
    }
}
