using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore; // OpenIddictServerAspNetCoreHelpers.GetOpenIddictServerRequest() lives here
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
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
/// OpenIddict's Authorization Code + PKCE endpoints (see ADR 0002 for why this replaced
/// ROPC): `/connect/authorize` (the redirect target the panel's PKCE login lands on),
/// `/connect/token` (code + refresh_token exchange), and a minimal, unstyled
/// `/connect/login` page backed by its own cookie scheme — the one piece of this API that
/// renders HTML instead of JSON, because a redirect-based OAuth flow needs somewhere for
/// the browser to actually authenticate.
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

    [HttpGet("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict request cannot be retrieved.");

        var result = await HttpContext.AuthenticateAsync(AuthCookieDefaults.Scheme);
        if (result?.Succeeded != true)
        {
            // Sends the browser to /connect/login?ReturnUrl=<this exact request>, so the
            // user lands right back here — with every OAuth param intact — once they
            // authenticate.
            return Challenge(
                new AuthenticationProperties { RedirectUri = Request.GetEncodedPathAndQuery() },
                AuthCookieDefaults.Scheme);
        }

        var userId = Guid.Parse(result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.IgnoreQueryFilters().SingleOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user is null)
        {
            // The cookie outlived the user (e.g. deleted after signing in) — drop the
            // stale session and force a fresh login rather than issuing a code for a
            // principal that no longer resolves to anyone.
            await HttpContext.SignOutAsync(AuthCookieDefaults.Scheme);
            return Challenge(
                new AuthenticationProperties { RedirectUri = Request.GetEncodedPathAndQuery() },
                AuthCookieDefaults.Scheme);
        }

        var identity = BuildIdentity(user);
        identity.SetScopes(request.GetScopes());

        // No consent screen: the panel is a trusted first-party client, not a
        // third-party app a user needs to approve.
        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            // Both grants re-authenticate via the scheme that already decoded (and
            // validated — rotation/reuse detection included) the code or refresh token
            // before this handler ever ran, then just re-issue from that same
            // principal — the claims were fixed at /connect/authorize (or the previous
            // token issuance) time, never re-derived here.
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (result.Principal is null)
            {
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid.",
                    }),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return Forbid(
            new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.UnsupportedGrantType,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified grant type is not supported.",
            }),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet(AuthCookieDefaults.LoginPath)]
    public IActionResult Login(string? returnUrl) => Content(LoginPageHtml(returnUrl, error: null), "text/html");

    [HttpPost(AuthCookieDefaults.LoginPath)]
    public async Task<IActionResult> LoginSubmit(
        [FromForm] string email, [FromForm] string password, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters: no tenant is resolved yet at login time (that's what this
        // login is *for*) — TenantAwareDbContext's filter would otherwise hide every
        // user, including the one trying to log in.
        var user = await _context.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);

        if (user is null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
            return Content(LoginPageHtml(returnUrl, error: "Invalid email or password."), "text/html");

        var cookieIdentity = new ClaimsIdentity(AuthCookieDefaults.Scheme);
        cookieIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        await HttpContext.SignInAsync(AuthCookieDefaults.Scheme, new ClaimsPrincipal(cookieIdentity));

        // Only ever redirect back into this server's own /connect/authorize — never an
        // arbitrary caller-supplied URL — to close off an open-redirect via `returnUrl`.
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl.StartsWith("/connect/authorize", StringComparison.Ordinal))
            return Redirect(returnUrl);

        return Redirect("/connect/authorize");
    }

    [HttpPost("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        // Ends the server-side login-page session so the next /connect/authorize visit
        // requires re-entering credentials, rather than silently re-issuing a code.
        // Revoking the still-valid refresh token itself is left to its own 14-day expiry
        // — see ADR 0002's Consequences for why that's a deliberate simplification here,
        // not an oversight.
        await HttpContext.SignOutAsync(AuthCookieDefaults.Scheme);
        return Ok();
    }

    private static string LoginPageHtml(string? returnUrl, string? error) => $$"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <title>Sign in — Peoplise</title>
        </head>
        <body style="font-family: system-ui, sans-serif; max-width: 320px; margin: 80px auto; padding: 0 16px;">
          <h1 style="font-size: 1.25rem;">Sign in to Peoplise</h1>
          {{(error is not null ? $"""<p style="color:#b91c1c;">{WebUtility.HtmlEncode(error)}</p>""" : "")}}
          <form method="post" action="{{AuthCookieDefaults.LoginPath}}">
            <input type="hidden" name="returnUrl" value="{{WebUtility.HtmlEncode(returnUrl ?? "")}}" />
            <label style="display:block;margin-bottom:8px;">
              Email<br />
              <input name="email" type="email" required autofocus style="width:100%;box-sizing:border-box;" />
            </label>
            <label style="display:block;margin-bottom:12px;">
              Password<br />
              <input name="password" type="password" required autocomplete="current-password" style="width:100%;box-sizing:border-box;" />
            </label>
            <button type="submit">Sign in</button>
          </form>
        </body>
        </html>
        """;

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
        // one whenever "openid" scope is requested — and unlike the access token
        // (encrypted, opaque by design — only this server can read it), the id_token is
        // signed-only and meant to be decoded by the client. See @peoplise/api-client's
        // tokenEndpoint.ts, which reads user info from the id_token for exactly that
        // reason.
        foreach (var claim in identity.Claims)
        {
            claim.SetDestinations(claim.Type == HttpContextTenantContext.TenantClaimType
                ? [Destinations.AccessToken]
                : [Destinations.AccessToken, Destinations.IdentityToken]);
        }

        return identity;
    }
}
