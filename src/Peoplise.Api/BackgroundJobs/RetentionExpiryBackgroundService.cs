using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Peoplise.Modules.HrBot.Application.Common;
using Peoplise.Modules.HrBot.Application.Conversations.Commands;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.Modules.VideoInterview.Application.Cases.Commands;
using Peoplise.Modules.VideoInterview.Application.Common;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Auditing;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Api.BackgroundJobs;

/// <summary>
/// KVKK's scheduled retention sweep: once a day (and once at startup), finds every
/// VideoInterview <see cref="Case"/> and HrBot <see cref="Conversation"/> whose project's
/// retention period has elapsed — across every tenant in one pass — and anonymizes each
/// by dispatching the same command a panel-triggered consent withdrawal uses. Lives here,
/// in the composition root, rather than inside either module, because it needs to know
/// about both and modules don't reference each other — see ADR 0001 (the original,
/// VideoInterview-only design) and ADR 0003 (why it moved here to cover HrBot too).
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
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var now = DateTimeOffset.UtcNow;

            using var _ = AmbientUserOverride.Begin(SystemUserId);
            await SweepCasesAsync(scope.ServiceProvider, mediator, now, cancellationToken);
            await SweepConversationsAsync(scope.ServiceProvider, mediator, now, cancellationToken);
        }
        catch (Exception ex)
        {
            // Logged and swallowed deliberately: one bad sweep must not crash the loop
            // and stop every future day's sweep from running.
            _logger.LogError(ex, "KVKK retention sweep failed.");
        }
    }

    private async Task SweepCasesAsync(IServiceProvider services, IMediator mediator, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var cases = services.GetRequiredService<IRepository<Case, CaseId>>();
        var caseBotProjects = services.GetRequiredService<IRepository<CaseBotProject, CaseBotProjectId>>();

        // Cross-tenant by design: retention expiry is a system-level KVKK obligation,
        // not one tenant's request. See ADR 0001.
        var projects = await caseBotProjects.ListAsync(_ => true, ignoreQueryFilters: true, cancellationToken);
        var retentionDaysByProject = projects.ToDictionary(p => p.Id, p => p.RetentionPeriodDays);

        var candidates = await cases.ListAsync(
            c => c.Status == CaseStatus.InProgress || c.Status == CaseStatus.Completed || c.Status == CaseStatus.TimedOut,
            ignoreQueryFilters: true,
            cancellationToken);

        var eligible = candidates
            .Where(c => retentionDaysByProject.TryGetValue(c.CaseBotProjectId, out var days)
                && CaseRetentionEligibility.IsEligible(c.Status, c.StartedAt, c.CompletedAt, days, now))
            .Select(c => (Id: c.Id.Value, c.TenantId))
            .ToList();

        _logger.LogInformation("KVKK retention sweep found {Count} case(s) eligible for expiry.", eligible.Count);

        foreach (var (id, tenantId) in eligible)
        {
            await ExpireOneAsync(
                mediator, "case", id, tenantId,
                () => new RequestDataDeletionCommand(id, DataDeletionReason.RetentionExpired, null),
                cancellationToken);
        }
    }

    private async Task SweepConversationsAsync(IServiceProvider services, IMediator mediator, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var conversations = services.GetRequiredService<IRepository<Conversation, ConversationId>>();
        var botProjects = services.GetRequiredService<IRepository<BotProject, BotProjectId>>();

        var projects = await botProjects.ListAsync(_ => true, ignoreQueryFilters: true, cancellationToken);
        var retentionDaysByProject = projects.ToDictionary(p => p.Id, p => p.RetentionPeriodDays);

        var candidates = await conversations.ListAsync(
            c => c.Status == ConversationStatus.InProgress || c.Status == ConversationStatus.Completed
                || c.Status == ConversationStatus.ScreenedOut || c.Status == ConversationStatus.TimedOut,
            ignoreQueryFilters: true,
            cancellationToken);

        var eligible = candidates
            .Where(c => retentionDaysByProject.TryGetValue(c.BotProjectId, out var days)
                && ConversationRetentionEligibility.IsEligible(c.Status, c.StartedAt, c.CompletedAt, days, now))
            .Select(c => (Id: c.Id.Value, c.TenantId))
            .ToList();

        _logger.LogInformation("KVKK retention sweep found {Count} conversation(s) eligible for expiry.", eligible.Count);

        foreach (var (id, tenantId) in eligible)
        {
            await ExpireOneAsync(
                mediator, "conversation", id, tenantId,
                () => new RequestConversationDataDeletionCommand(id, ConversationDataDeletionReason.RetentionExpired, null),
                cancellationToken);
        }
    }

    private async Task ExpireOneAsync(
        IMediator mediator, string kind, Guid id, Guid tenantId, Func<IRequest<Result>> commandFactory, CancellationToken cancellationToken)
    {
        try
        {
            // The eligible-entity query above deliberately reads across every tenant
            // (IgnoreQueryFilters), but the deletion command handler loads the entity
            // through the normal, tenant-filtered repository — a background job has no
            // per-request tenant to resolve, so without this override every lookup would
            // come back "not found" regardless of which tenant it belongs to.
            using var _ = AmbientTenantOverride.Begin(TenantId.From(tenantId));

            var result = await mediator.Send(commandFactory(), cancellationToken);

            if (result.IsFailure)
                _logger.LogWarning("Could not expire retention for {Kind} {Id}: {Error}", kind, id, result.Error.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to expire retention for {Kind} {Id}.", kind, id);
        }
    }
}
