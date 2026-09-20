using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Peoplise.SharedKernel.Auditing;

namespace Peoplise.Infrastructure.Identity;

/// <summary>
/// Resolves the current user from the standard <see cref="ClaimTypes.NameIdentifier"/>
/// (OpenIddict maps the token's <c>sub</c> claim here), unless
/// <see cref="AmbientUserOverride.Current"/> is set — a background job or seed script
/// runs as "system" with no HTTP request to read a claim from, so it sets that ambient
/// override instead of needing its own <see cref="ICurrentUserContext"/> implementation.
/// <c>null</c> when neither applies.
/// </summary>
public sealed class HttpContextCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        AmbientUserOverride.Current
        ?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
