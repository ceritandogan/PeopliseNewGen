using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Peoplise.SharedKernel.Auditing;

namespace Peoplise.Infrastructure.Identity;

/// <summary>
/// Resolves the current user from the standard <see cref="ClaimTypes.NameIdentifier"/>
/// (OpenIddict maps the token's <c>sub</c> claim here). <c>null</c> outside an
/// authenticated HTTP request — background jobs and seed scripts run as "system" and
/// should pass their own <see cref="ICurrentUserContext"/> implementation instead of
/// this one.
/// </summary>
public sealed class HttpContextCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
