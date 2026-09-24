using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Anthropic;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Peoplise.Infrastructure.AI;
using Peoplise.Infrastructure.Events;
using Peoplise.Infrastructure.Identity;
using Peoplise.Infrastructure.Media;
using Peoplise.Infrastructure.Modules;
using Peoplise.Infrastructure.Notifications;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Infrastructure.Persistence.Interceptors;
using Peoplise.Infrastructure.Security;
using Peoplise.Infrastructure.Validation;
using Peoplise.SharedKernel.AI;
using Peoplise.SharedKernel.Auditing;
using Peoplise.SharedKernel.Domain;
using Peoplise.SharedKernel.Events;
using Peoplise.SharedKernel.Media;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Notifications;
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
        IHostEnvironment environment,
        params Assembly[] moduleAssemblies)
    {
        var registry = new ModuleAssemblyRegistry(moduleAssemblies);
        services.AddSingleton(registry);

        // "Testing" (the WebApplicationFactory-based integration test host, see
        // Peoplise.Api.Tests.ApiTestFactory) gets the same dev-only auth fallbacks as
        // Development — ephemeral certs, no HTTPS requirement — without also tripping
        // Program.cs's IsDevelopment()-gated migration/seed block, which assumes a real
        // relational database the in-memory test host doesn't have.
        var useDevelopmentAuthDefaults = environment.IsDevelopment() || environment.IsEnvironment("Testing");

        services.AddPersistence(configuration);
        services.AddDomainEvents(registry);
        services.AddAuth(configuration, useDevelopmentAuthDefaults);
        services.AddSingleton<ICandidateResourceTokenService, HmacCandidateResourceTokenService>();
        services.AddMediaAndAI(configuration);
        services.AddNotifications(configuration);

        return services;
    }

    /// <summary>
    /// File storage is a real, working local-disk implementation — fine for
    /// development, not for any shared environment (see <see cref="LocalFileStorageService"/>).
    /// The AI provider is real (<see cref="AnthropicAIProvider"/>, code review only — see
    /// its own remarks on why transcription stays unimplemented) once
    /// <c>AI:Anthropic:ApiKey</c> is configured (<c>scripts/setup-anthropic-key.sh</c>
    /// walks through getting one); falls back to the loud-failure placeholder otherwise,
    /// the same "safe until configured" shape <c>Auth:Certificates</c> uses.
    /// </summary>
    private static IServiceCollection AddMediaAndAI(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        var anthropic = configuration.GetSection("AI:Anthropic");
        var apiKey = anthropic["ApiKey"];

        if (!string.IsNullOrEmpty(apiKey))
        {
            var model = anthropic["Model"] is { Length: > 0 } configuredModel ? configuredModel : "claude-haiku-4-5-20251001";
            services.AddSingleton(new AnthropicClient { ApiKey = apiKey });
            services.AddSingleton<IAIProvider>(sp => new AnthropicAIProvider(sp.GetRequiredService<AnthropicClient>(), model));
        }
        else
        {
            services.AddSingleton<IAIProvider, NotConfiguredAIProvider>();
        }

        return services;
    }

    /// <summary>
    /// The real email sender (<see cref="SmtpEmailSender"/>, plain SMTP via MailKit —
    /// provider-agnostic, works against any SMTP endpoint including a Mailtrap sandbox
    /// for local verification) once <c>Email:Smtp:Host</c> is configured
    /// (<c>scripts/setup-email-smtp.sh</c> walks through a Mailtrap sandbox); falls back
    /// to the loud-failure placeholder otherwise — the same "safe until configured" shape
    /// <see cref="AddMediaAndAI"/> uses for <see cref="IAIProvider"/>.
    /// </summary>
    private static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        var smtp = configuration.GetSection("Email:Smtp");
        var host = smtp["Host"];

        if (!string.IsNullOrEmpty(host))
        {
            var options = new SmtpOptions(
                Host: host,
                Port: int.TryParse(smtp["Port"], out var port) ? port : 587,
                Username: smtp["Username"] ?? string.Empty,
                Password: smtp["Password"] ?? string.Empty,
                FromAddress: smtp["FromAddress"] ?? string.Empty,
                FromName: smtp["FromName"] ?? string.Empty,
                UseSsl: !bool.TryParse(smtp["UseSsl"], out var useSsl) || useSsl);
            services.AddSingleton(options);
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, NotConfiguredEmailSender>();
        }

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
    /// Authorization Code + PKCE is the only interactive grant (see ADR 0002 for why ROPC
    /// was removed rather than kept alongside it); refresh_token stays for silent renewal.
    /// </summary>
    private static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore().UseDbContext<AppDbContext>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("connect/authorize");
                options.SetTokenEndpointUris("connect/token");

                options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
                options.AllowRefreshTokenFlow();

                options.RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.OfflineAccess);

                // Unconfigured before this — OpenIddict's library defaults applied
                // silently. Pinned explicitly now that a real deployment is in view.
                options.SetAccessTokenLifetime(TimeSpan.FromHours(1));
                options.SetIdentityTokenLifetime(TimeSpan.FromMinutes(5));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(14));

                // OpenIddict rotates refresh tokens and rejects a reused/revoked one by
                // default — the "custom JWT" risk flagged in the architecture review
                // (hand-rolled refresh rotation/reuse detection) is handled by the
                // library, not by code in this solution.

                // Real cert/key store, generic and swappable rather than tied to one
                // cloud provider's KMS — a file path + password sourced from config
                // (env-var-backed in any real deployment, following the same convention
                // as ConnectionStrings:Default). Falls back to OpenIddict's ephemeral
                // development certificates only in Development, and refuses to start
                // without real ones anywhere else.
                var certificates = configuration.GetSection("Auth:Certificates");
                var signingPath = certificates["SigningCertificatePath"];
                var encryptionPath = certificates["EncryptionCertificatePath"];

                if (!string.IsNullOrEmpty(signingPath) && !string.IsNullOrEmpty(encryptionPath))
                {
                    options.AddSigningCertificate(
                        X509CertificateLoader.LoadPkcs12FromFile(signingPath, certificates["SigningCertificatePassword"]));
                    options.AddEncryptionCertificate(
                        X509CertificateLoader.LoadPkcs12FromFile(encryptionPath, certificates["EncryptionCertificatePassword"]));
                }
                else if (isDevelopment)
                {
                    options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
                }
                else
                {
                    throw new InvalidOperationException(
                        "Auth:Certificates:SigningCertificatePath and EncryptionCertificatePath must be configured outside Development.");
                }

                var aspNetCore = options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough();

                // TODO(auth): local HTTP-only dev only. OpenIddict requires HTTPS by
                // default specifically because a bearer token sent over plain HTTP is
                // trivially interceptable — this must stay enabled everywhere but a
                // local machine.
                if (isDevelopment)
                    aspNetCore.DisableTransportSecurityRequirement();
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
            })
            // Backs only the server-hosted `/connect/login` page (see AuthorizationController
            // and ADR 0002) — every other endpoint keeps validating Bearer tokens via the
            // default scheme above; this is never the default.
            .AddCookie(AuthCookieDefaults.Scheme, options =>
            {
                options.LoginPath = AuthCookieDefaults.LoginPath;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            });

        services.AddAuthorization();

        return services;
    }
}
