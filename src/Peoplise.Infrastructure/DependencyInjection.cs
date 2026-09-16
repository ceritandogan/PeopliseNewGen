using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using Peoplise.Infrastructure.AI;
using Peoplise.Infrastructure.Events;
using Peoplise.Infrastructure.Identity;
using Peoplise.Infrastructure.Media;
using Peoplise.Infrastructure.Modules;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Infrastructure.Persistence.Interceptors;
using Peoplise.Infrastructure.Validation;
using Peoplise.SharedKernel.AI;
using Peoplise.SharedKernel.Auditing;
using Peoplise.SharedKernel.Domain;
using Peoplise.SharedKernel.Events;
using Peoplise.SharedKernel.Media;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Persistence;

namespace Peoplise.Infrastructure;

/// <summary>
/// Wires up everything this layer provides: persistence (<see cref="AppDbContext"/>,
/// interceptors, generic repository, unit of work), in-process domain event dispatch,
/// and authentication (OpenIddict acting as both authorization server and resource
/// server for this monolith — see the <c>AddAuth</c> region for why, and what to revisit
/// before this goes to production).
/// </summary>
public static class DependencyInjection
{
    /// <param name="moduleAssemblies">
    /// Each business module's assembly (e.g. <c>Peoplise.Modules.ATS</c>) — supplied by
    /// the Api composition root, not discovered by Infrastructure itself. Used to find
    /// MediatR command/query handlers, FluentValidation validators, and EF Core
    /// <c>IEntityTypeConfiguration&lt;T&gt;</c> classes via reflection, without
    /// Infrastructure ever taking a compile-time reference on a module. See
    /// <see cref="ModuleAssemblyRegistry"/>.
    /// </param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] moduleAssemblies)
    {
        var registry = new ModuleAssemblyRegistry(moduleAssemblies);
        services.AddSingleton(registry);

        services.AddPersistence(configuration);
        services.AddDomainEvents(registry);
        services.AddAuth();
        services.AddMediaAndAI();

        return services;
    }

    /// <summary>
    /// File storage is a real, working local-disk implementation — fine for
    /// development, not for any shared environment (see <see cref="LocalFileStorageService"/>).
    /// The AI provider is intentionally unimplemented (see <see cref="NotConfiguredAIProvider"/>)
    /// until a real Azure OpenAI/OpenAI/Claude integration and its credentials exist.
    /// </summary>
    private static IServiceCollection AddMediaAndAI(this IServiceCollection services)
    {
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddSingleton<IAIProvider, NotConfiguredAIProvider>();

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is not configured. Set ConnectionStrings:Default.");

        services.AddScoped<TenantInterceptor>();
        services.AddScoped<AuditInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString);

            // Registers OpenIddict's client/token/scope/authorization entity sets on
            // this same context — see the AddAuth region below.
            options.UseOpenIddict();

            options.AddInterceptors(
                serviceProvider.GetRequiredService<TenantInterceptor>(),
                serviceProvider.GetRequiredService<AuditInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<,>), typeof(GenericRepository<,>));

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpContextTenantContext>();
        services.AddScoped<ICurrentUserContext, HttpContextCurrentUserContext>();

        return services;
    }

    private static IServiceCollection AddDomainEvents(this IServiceCollection services, ModuleAssemblyRegistry registry)
    {
        var assemblies = registry.Assemblies.Append(typeof(DependencyInjection).Assembly).ToArray();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssemblies(assemblies);

        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

        return services;
    }

    /// <summary>
    /// OpenIddict as a self-hosted authorization server (issues tokens) *and* resource
    /// server (validates them), both inside this same API Gateway process — deliberately
    /// not hand-rolled JWT signing/validation (see the architecture doc's rationale).
    /// </summary>
    /// <remarks>
    /// Two things are intentionally left as placeholders here, both flagged with TODOs:
    /// the resource-owner-password-credentials grant, and the development-only signing
    /// certificate. Both are fine for building out the panel/candidate apps against a
    /// first-party API, but neither belongs in production as-is — see the TODOs.
    /// </remarks>
    private static IServiceCollection AddAuth(this IServiceCollection services)
    {
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore().UseDbContext<AppDbContext>();
            })
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("connect/token");

                // TODO(auth): ROPC (password grant) is used because both first-party
                // clients (panel, candidate app) are built and controlled by this team
                // and there's no third-party client yet. Once a browser-based login UI
                // exists, migrate panel/candidate to Authorization Code + PKCE instead —
                // ROPC means the client handles the user's raw password, which is only
                // acceptable for fully first-party, fully trusted clients.
                options.AllowPasswordFlow();
                options.AllowRefreshTokenFlow();

                // The panel/candidate apps are first-party, trusted clients with no
                // OpenIddict client registration of their own (no client_id/secret) —
                // this accepts token requests without one, appropriate only because
                // every caller is code we control (see the ROPC TODO above).
                options.AcceptAnonymousClients();

                // OpenIddict rotates refresh tokens and rejects a reused/revoked one by
                // default — the "custom JWT" risk flagged in the architecture review
                // (hand-rolled refresh rotation/reuse detection) is handled by the
                // library, not by code in this solution.

                // TODO(auth): development-only certificates. Replace with a real
                // signing/encryption certificate (or a managed key store) before any
                // non-local deployment — these are regenerated on every restart and are
                // not intended to ever hold production tokens.
                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                // TODO(auth): local HTTP-only dev. Remove DisableTransportSecurityRequirement()
                // once this runs behind HTTPS anywhere but a local machine — OpenIddict
                // requires HTTPS by default specifically because a bearer token sent over
                // plain HTTP is trivially interceptable.
                options.UseAspNetCore()
                    .EnableTokenEndpointPassthrough()
                    .DisableTransportSecurityRequirement();
            })
            .AddValidation(options =>
            {
                // Local validation (no network round-trip to introspect): the server
                // and the resource API are the same process, so this is just checking
                // the token's signature against the server's own signing key.
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        services.AddAuthorization();

        return services;
    }
}
