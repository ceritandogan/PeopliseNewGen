namespace Peoplise.Infrastructure.Identity;

/// <summary>
/// The cookie authentication scheme backing the server-hosted `/connect/login` page —
/// distinct from the Bearer/OpenIddict-validation scheme every other API call uses (that
/// stays the ASP.NET Core default), and scoped tightly to completing the
/// Authorization Code + PKCE redirect at `/connect/authorize`. See ADR 0002.
/// </summary>
public static class AuthCookieDefaults
{
    public const string Scheme = "Peoplise.AuthCookie";
    public const string LoginPath = "/connect/login";
}
