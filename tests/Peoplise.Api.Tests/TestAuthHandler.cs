using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Peoplise.Infrastructure.Identity;

namespace Peoplise.Api.Tests;

/// <summary>
/// Bypasses real OAuth validation entirely for the test host — see ADR 0002's
/// Consequences: removing ROPC means curl-style `grant_type=password` token acquisition
/// no longer works, so automated tests authenticate via this standard
/// WebApplicationFactory pattern instead. Reads claims from request headers (not static
/// mutable state) so tests stay safe to run in parallel.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string UserIdHeader = "X-Test-UserId";
    public const string TenantIdHeader = "X-Test-TenantId";
    public const string RolesHeader = "X-Test-Roles";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };

        if (Request.Headers.TryGetValue(TenantIdHeader, out var tenantId))
            claims.Add(new Claim(HttpContextTenantContext.TenantClaimType, tenantId.ToString()));

        if (Request.Headers.TryGetValue(RolesHeader, out var roles))
        {
            foreach (var role in roles.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries))
                claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
