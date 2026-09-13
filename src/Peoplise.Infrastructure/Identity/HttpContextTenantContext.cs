using Microsoft.AspNetCore.Http;
using Peoplise.SharedKernel.MultiTenancy;

namespace Peoplise.Infrastructure.Identity;

/// <summary>
/// Resolves the current tenant from the <c>tenant_id</c> claim on the authenticated
/// user's access token. Registered per-request (scoped); returns <c>null</c> before
/// authentication runs or for a request with no tenant claim (e.g. the token endpoint
/// itself), which the tenant interceptor treats as "reject any write".
/// </summary>
public sealed class HttpContextTenantContext : ITenantContext
{
    public const string TenantClaimType = "tenant_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public TenantId? TenantId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(TenantClaimType);
            return claim is not null && Guid.TryParse(claim.Value, out var value)
                ? Peoplise.SharedKernel.MultiTenancy.TenantId.From(value)
                : null;
        }
    }
}
