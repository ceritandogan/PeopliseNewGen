using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Peoplise.Infrastructure.Persistence;

namespace Peoplise.Api.Tests;

/// <summary>
/// Boots the real <c>Program</c> composition root against an isolated in-memory
/// <see cref="AppDbContext"/> (a fresh database per factory instance, so tests don't leak
/// state into each other) and swaps the default authentication scheme for
/// <see cref="TestAuthHandler"/> — the real OpenIddict pipeline (certs, client
/// registration, redirect flow) isn't something a unit-style test host should have to
/// stand up. See ADR 0002.
/// </summary>
public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    // Captured once per factory instance rather than inline in the options lambda below
    // — that lambda can run more than once (e.g. once per resolved scope), and each call
    // generating its own Guid would silently scatter seeded data across multiple,
    // mutually invisible in-memory databases instead of one shared one.
    private readonly string _databaseName = $"peoplise-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" — not "Development" — specifically so Program.cs's
        // IsDevelopment()-gated migration/seed block (which assumes a real relational
        // database) never runs against this in-memory one. DependencyInjection.AddAuth
        // separately treats "Testing" the same as Development for its own dev-only
        // fallbacks (ephemeral certs, relaxed cookie/transport requirements).
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Removing just DbContextOptions<AppDbContext> isn't enough: AddDbContext
            // also registers an IDbContextOptionsConfiguration<AppDbContext> that still
            // carries the original UseNpgsql(...) configuration, and EF refuses to boot
            // with two providers configured for the same context. Both have to go.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                // Registers OpenIddict's client/token/scope/authorization entity sets on
                // the model — dropped otherwise, since this replaces the original
                // AddDbContext call (with its own UseOpenIddict()) rather than extending it.
                options.UseOpenIddict();
            });

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });
        });
    }
}
