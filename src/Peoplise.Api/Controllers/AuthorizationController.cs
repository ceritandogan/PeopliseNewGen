using System.Security.Claims;
using Microsoft.AspNetCore; // OpenIddictServerAspNetCoreHelpers.GetOpenIddictServerRequest() lives here
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Peoplise.Infrastructure.Identity;
using Peoplise.Infrastructure.Persistence;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Peoplise.Api.Controllers;

/// <summary>
/// The `/connect/token` handler OpenIddict's server has been configured to expect since
/// Stage 1, but never had behind it until now — this is what actually validates
/// credentials and issues tokens. Handles password grant (login) and refresh_token
/// grant (silent refresh); anything else 400s.
/// </summary>
[ApiController]
public sealed class AuthorizationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthorizationController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict request cannot be retrieved.");

        if (request.IsPasswordGrantType())
            return await ExchangePasswordGrantAsync(request, cancellationToken);

        if (request.IsRefreshTokenGrantType())
            return await ExchangeRefreshTokenGrantAsync();

        return Forbid(
            new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.UnsupportedGrantType,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified grant type is not supported.",
            }),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> ExchangePasswordGrantAsync(OpenIddictRequest request, CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters: no tenant is resolved yet at login time (that's what this
        // token issuance is *for*) — TenantAwareDbContext's filter would otherwise hide
        // every user, including the one trying to log in.
        var user = await _context.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(u => u.Email == request.Username && !u.IsDeleted, cancellationToken);

        if (user is null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password!) == PasswordVerificationResult.Failed)
        {
            return Forbid(
                new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid email or password.",
                }),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var identity = BuildIdentity(user);
        identity.SetScopes(request.GetScopes());

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> ExchangeRefreshTokenGrantAsync()
    {
        // OpenIddict re-validates the refresh token itself before this handler even
        // runs (rotation/reuse detection included — see the architecture doc's auth
        // rationale); this just re-issues from the principal it already authenticated.
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (result.Principal is null)
        {
            return Forbid(
                new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is no longer valid.",
                }),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static ClaimsIdentity BuildIdentity(User user)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(Claims.Email, user.Email);
        identity.SetClaim(Claims.Name, user.DisplayName);
        identity.SetClaim(HttpContextTenantContext.TenantClaimType, user.TenantId.ToString());

        foreach (var role in user.Roles)
            identity.AddClaim(Claims.Role, role);

        // tenant_id is access-token-only: it's what HttpContextTenantContext reads on
        // every API call, and has no reason to be readable client-side. Everything
        // else (sub/email/name/role) also goes into the id_token — OpenIddict issues
        // one even for this ROPC flow whenever "openid" scope is requested, and unlike
        // the access token (encrypted, opaque by design — only this server can read
        // it), the id_token is signed-only and meant to be decoded by the client. See
        // @peoplise/api-client's tokenEndpoint.ts, which reads user info from the
        // id_token for exactly that reason.
        foreach (var claim in identity.Claims)
        {
            claim.SetDestinations(claim.Type == HttpContextTenantContext.TenantClaimType
                ? [Destinations.AccessToken]
                : [Destinations.AccessToken, Destinations.IdentityToken]);
        }

        return identity;
    }
}
