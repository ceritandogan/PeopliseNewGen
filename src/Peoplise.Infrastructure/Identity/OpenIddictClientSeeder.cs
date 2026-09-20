using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Peoplise.Infrastructure.Identity;

/// <summary>
/// Registers the panel app as an OpenIddict client — replaces the old
/// <c>AcceptAnonymousClients()</c> escape hatch (see ADR 0002) with a real, PKCE-required
/// public client. Runs in every environment (unlike <c>DatabaseSeeder</c>, which is
/// Development-only): without this row the app has no way to log in anywhere. Idempotent
/// and safe under concurrent startup (e.g. multiple replicas racing to seed).
/// </summary>
public static class OpenIddictClientSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var manager = services.GetRequiredService<IOpenIddictApplicationManager>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(OpenIddictClientSeeder).FullName!);

        var clientId = configuration["Auth:Panel:ClientId"] ?? "peoplise-panel";
        var redirectUri = configuration["Auth:Panel:RedirectUri"] ?? "http://localhost:5173/auth/callback";
        var postLogoutRedirectUri = configuration["Auth:Panel:PostLogoutRedirectUri"] ?? "http://localhost:5173/login";

        if (await manager.FindByClientIdAsync(clientId, cancellationToken) is not null)
            return;

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = ClientTypes.Public,
            DisplayName = "Peoplise Panel",
            RedirectUris = { new Uri(redirectUri) },
            PostLogoutRedirectUris = { new Uri(postLogoutRedirectUri) },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Prefixes.Scope + Scopes.OfflineAccess,
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange },
        };

        try
        {
            await manager.CreateAsync(descriptor, cancellationToken);
        }
        catch (Exception ex)
        {
            // Another replica may have won the race to create the same client_id between
            // our existence check and this call — re-check rather than crash startup.
            if (await manager.FindByClientIdAsync(clientId, cancellationToken) is null)
                throw;

            logger.LogInformation(ex, "OpenIddict client '{ClientId}' was created concurrently by another instance.", clientId);
        }
    }
}
