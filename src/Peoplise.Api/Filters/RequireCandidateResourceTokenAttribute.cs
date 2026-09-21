using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Peoplise.Infrastructure.Security;

namespace Peoplise.Api.Filters;

/// <summary>
/// Guards a candidate-facing write endpoint that would otherwise trust a bare resource
/// GUID: requires the caller to present a valid <see cref="ICandidateResourceTokenService"/>
/// token, in the <see cref="TokenHeader"/> header, bound to exactly the resource named by
/// <paramref name="routeParameterName"/> in the route. See ADR 0004.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireCandidateResourceTokenAttribute : Attribute, IActionFilter
{
    public const string TokenHeader = "X-Candidate-Token";

    private readonly CandidateResourceType _resourceType;
    private readonly string _routeParameterName;

    public RequireCandidateResourceTokenAttribute(CandidateResourceType resourceType, string routeParameterName)
    {
        _resourceType = resourceType;
        _routeParameterName = routeParameterName;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.RouteData.Values.TryGetValue(_routeParameterName, out var routeValue)
            || !Guid.TryParse(routeValue?.ToString(), out var resourceId))
        {
            context.Result = new BadRequestObjectResult($"Missing or invalid route value '{_routeParameterName}'.");
            return;
        }

        var token = context.HttpContext.Request.Headers[TokenHeader].FirstOrDefault();
        var tokenService = context.HttpContext.RequestServices.GetRequiredService<ICandidateResourceTokenService>();

        if (!tokenService.TryValidate(token, _resourceType, resourceId, out var error))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = error,
                Status = StatusCodes.Status401Unauthorized,
            })
            { StatusCode = StatusCodes.Status401Unauthorized };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
