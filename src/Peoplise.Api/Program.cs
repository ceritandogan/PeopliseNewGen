using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.EntityFrameworkCore;
using Peoplise.Api;
using Peoplise.Api.BackgroundJobs;
using Peoplise.Api.Middleware;
using Peoplise.Api.Services;
using Peoplise.Infrastructure;
using Peoplise.Infrastructure.Identity;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Application.Positions.Commands;
using Peoplise.Modules.HrBot.Application.Conversations.Commands;
using Peoplise.Modules.VideoInterview;
using Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Commands;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog: structured logging, configurable via the "Serilog" appsettings section;
// console sink is the Stage-1 baseline, swap/add sinks (e.g. Seq, OTLP) per environment.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// OpenTelemetry: tracing + metrics. Console exporter for now — swap for an OTLP
// exporter once a collector (e.g. the .NET Aspire dashboard, Grafana Tempo) exists.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: "Peoplise.Api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

// String enums over the wire (e.g. "Hybrid", not a number) — matches the frontend's
// TypeScript union types, which are all string literals.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Peoplise API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token issued by /connect/token.",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        },
    });
});

// SharedKernel/Infrastructure/persistence/auth wiring — see Peoplise.Infrastructure.DependencyInjection.
// Each business module's assembly is passed in here so Infrastructure can discover its
// MediatR handlers, FluentValidation validators, and EF Core entity configurations by
// reflection, without ever referencing the module directly (see ModuleAssemblyRegistry).
builder.Services.AddInfrastructure(
    builder.Configuration,
    builder.Environment,
    typeof(CreatePositionCommand).Assembly,
    typeof(StartConversationCommand).Assembly,
    typeof(CreateCaseBotProjectCommand).Assembly);

// VideoInterview needs its own explicit registration beyond the generic discovery
// above: an in-process queue + background worker for video transcription, which
// reflection-based scanning can't wire up on its own. See
// Peoplise.Modules.VideoInterview.DependencyInjection.
builder.Services.AddVideoInterviewModule();

// Composition-root service — spans ATS/HrBot/VideoInterview/Infrastructure, so it
// can't live in any one module. See CandidateLinkMailer's own remarks.
builder.Services.AddScoped<ICandidateLinkMailer, CandidateLinkMailer>();

// Lives here, not inside either module, because it sweeps both VideoInterview's Case
// and HrBot's Conversation — see ADR 0003.
builder.Services.AddHostedService<RetentionExpiryBackgroundService>();

// The trigger ActivateAfterDelay stage rules need — see the service's own remarks.
builder.Services.AddHostedService<WorkflowStageDelaySweepBackgroundService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Fixed-window per caller (authenticated user if present, else remote IP) —
    // a simple, deliberately conservative default; tune per-endpoint policies once
    // real traffic patterns exist.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var partitionKey = httpContext.User.Identity?.Name
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
});

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Default") ?? string.Empty,
        name: "postgres");

var app = builder.Build();

// Development convenience only: applies pending migrations and seeds the demo
// tenant/user on startup, so there's no separate manual step to get a runnable local
// database. Never do this automatically outside Development — migrations belong in a
// deliberate deploy step everywhere else.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(context);
    await DemoDataSeeder.SeedAsync(context);
}

// Unlike the block above, this runs in every environment: without a registered
// OpenIddict client the app has no way to log in anywhere at all (see ADR 0002).
// Idempotent, so it's safe to run on every startup of every replica.
{
    using var scope = app.Services.CreateScope();
    await OpenIddictClientSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Default");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

// Exposed for WebApplicationFactory-based integration tests in a later stage.
public partial class Program
{
}
