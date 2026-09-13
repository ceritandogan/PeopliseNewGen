using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Peoplise.Api.Middleware;
using Peoplise.Infrastructure;
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

builder.Services.AddControllers();

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
    typeof(CreatePositionCommand).Assembly,
    typeof(StartConversationCommand).Assembly,
    typeof(CreateCaseBotProjectCommand).Assembly);

// VideoInterview needs its own explicit registration beyond the generic discovery
// above: an in-process queue + background worker for video transcription, which
// reflection-based scanning can't wire up on its own. See
// Peoplise.Modules.VideoInterview.DependencyInjection.
builder.Services.AddVideoInterviewModule();

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
