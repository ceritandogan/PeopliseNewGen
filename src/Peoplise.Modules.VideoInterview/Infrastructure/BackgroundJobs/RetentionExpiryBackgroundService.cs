using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Peoplise.Modules.VideoInterview.Application.Cases.Commands;
using Peoplise.Modules.VideoInterview.Application.Common;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Auditing;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Persistence;

namespace Peoplise.Modules.VideoInterview.Infrastructure.BackgroundJobs;

/// <summary>
/// KVKK Section G's scheduled retention sweep: once a day (and once at startup), finds
/// every <see cref="Case"/> whose project's retention period has elapsed — across every
/// tenant in one pass — and anonymizes it by dispatching
/// <see cref="RequestDataDeletionCommand"/>, the same command a panel-triggered consent
/// withdrawal uses. See ADR 0001 for why: reusing that command means the
/// file-deletion-before-domain-call ordering only has to be correct in one place.
/// </summary>
public sealed class RetentionExpiryBackgroundService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(24);
    private const string SystemUserId = "system:kvkk-retention-job";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RetentionExpiryBackgroundService> _logger;

    public RetentionExpiryBackgroundService(IServiceScopeFactory scopeFactory, ILogger<RetentionExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SweepInterval);

        do
        {
            await RunSweepAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunSweepAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var cases = scope.ServiceProvider.GetRequiredService<IRepository<Case, CaseId>>();
            var caseBotProjects = scope.ServiceProvider.GetRequiredService<IRepository<CaseBotProject, CaseBotProjectId>>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var now = DateTimeOffset.UtcNow;

            // Cross-tenant by design: retention expiry is a system-level KVKK
            // obligation, not one tenant's request. See ADR 0001.
            var projects = await caseBotProjects.ListAsync(_ => true, ignoreQueryFilters: true, cancellationToken);
            var retentionDaysByProject = projects.ToDictionary(p => p.Id, p => p.RetentionPeriodDays);

            var candidates = await cases.ListAsync(
                c => c.Status == CaseStatus.InProgress || c.Status == CaseStatus.Completed || c.Status == CaseStatus.TimedOut,
                ignoreQueryFilters: true,
                cancellationToken);

            var eligible = candidates
                .Where(c => retentionDaysByProject.TryGetValue(c.CaseBotProjectId, out var days)
                    && CaseRetentionEligibility.IsEligible(c.Status, c.StartedAt, c.CompletedAt, days, now))
                .Select(c => (CaseId: c.Id.Value, TenantId: c.TenantId))
                .ToList();

            _logger.LogInformation("KVKK retention sweep found {Count} case(s) eligible for expiry.", eligible.Count);

            using var _ = AmbientUserOverride.Begin(SystemUserId);
            foreach (var (caseId, tenantId) in eligible)
                await ExpireOneAsync(mediator, caseId, tenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            // Logged and swallowed deliberately: one bad sweep must not crash the loop
            // and stop every future day's sweep from running.
            _logger.LogError(ex, "KVKK retention sweep failed.");
        }
    }

    private async Task ExpireOneAsync(IMediator mediator, Guid caseId, Guid tenantId, CancellationToken cancellationToken)
    {
        try
        {
            // The eligible-case query above deliberately reads across every tenant
            // (IgnoreQueryFilters), but RequestDataDeletionCommandHandler loads the case
            // through the normal, tenant-filtered repository — a background job has no
            // per-request tenant to resolve, so without this override every lookup would
            // come back "not found" regardless of which tenant the case belongs to.
            using var __ = AmbientTenantOverride.Begin(TenantId.From(tenantId));

            var result = await mediator.Send(
                new RequestDataDeletionCommand(caseId, DataDeletionReason.RetentionExpired, null), cancellationToken);

            if (result.IsFailure)
                _logger.LogWarning("Could not expire retention for case {CaseId}: {Error}", caseId, result.Error.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to expire retention for case {CaseId}.", caseId);
        }
    }
}
