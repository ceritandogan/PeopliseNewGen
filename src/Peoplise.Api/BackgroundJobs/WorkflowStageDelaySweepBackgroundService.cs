using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Peoplise.Modules.ATS.Application.Candidates.Commands;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Persistence;

namespace Peoplise.Api.BackgroundJobs;

/// <summary>
/// The trigger <see cref="StageRuleType.ActivateAfterDelay"/> rules need but have no
/// event to hook — nothing happens in the system when time simply passes, so this checks
/// for it directly. Same shape as <c>RetentionExpiryBackgroundService</c>'s daily,
/// cross-tenant sweep: find every candidate sitting on a stage whose delay rule has
/// elapsed, and run the normal stage-transition check for each. Score-based rules don't
/// need this — see <c>EvaluationSubmittedEventHandler</c>, which reacts to an event
/// instead of polling.
/// </summary>
public sealed class WorkflowStageDelaySweepBackgroundService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkflowStageDelaySweepBackgroundService> _logger;

    public WorkflowStageDelaySweepBackgroundService(
        IServiceScopeFactory scopeFactory, ILogger<WorkflowStageDelaySweepBackgroundService> logger)
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
            var processes = scope.ServiceProvider.GetRequiredService<IRepository<CandidateProcess, CandidateProcessId>>();
            var workflows = scope.ServiceProvider.GetRequiredService<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var now = DateTimeOffset.UtcNow;

            // Cross-tenant by design, same reasoning as the KVKK sweep: this is a
            // system-level scheduled check, not one tenant's request.
            var activeProcesses = await processes.ListAsync(
                p => p.CurrentStageId != null && !TerminalStatuses.Contains(p.Status), ignoreQueryFilters: true, cancellationToken);

            // Loads every workflow unconditionally rather than filtering by the active
            // processes' WorkflowDefinitionIds in the query itself — same choice
            // RetentionExpiryBackgroundService.SweepCasesAsync makes for CaseBotProject,
            // to avoid relying on EF translating .Contains() against a value-converted
            // id column (a real footgun class in this codebase — see GenericRepository's
            // own history). The workflow table is small (one per position); an
            // in-memory dictionary lookup afterward is simpler and just as correct.
            var allWorkflows = await workflows.ListAsync(_ => true, ignoreQueryFilters: true, cancellationToken);
            var workflowById = allWorkflows.ToDictionary(w => w.Id);

            var eligible = new List<(Guid ProcessId, Guid TenantId)>();
            foreach (var process in activeProcesses)
            {
                if (!workflowById.TryGetValue(process.WorkflowDefinitionId, out var workflow))
                    continue;

                var stage = workflow.Stages.FirstOrDefault(s => s.Id == process.CurrentStageId!.Value);
                if (stage is null)
                    continue;

                var daysInStage = (now - process.CurrentStageEnteredAt).TotalDays;
                var delayElapsed = stage.Rules.Any(r => r.Type == StageRuleType.ActivateAfterDelay && daysInStage >= r.DelayDays!.Value);

                if (delayElapsed)
                    eligible.Add((process.Id.Value, process.TenantId));
            }

            _logger.LogInformation("Workflow stage delay sweep found {Count} candidate process(es) eligible for auto-advance.", eligible.Count);

            foreach (var (processId, tenantId) in eligible)
                await TransitionOneAsync(mediator, processId, tenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            // Logged and swallowed deliberately: one bad sweep must not crash the loop
            // and stop every future day's sweep from running.
            _logger.LogError(ex, "Workflow stage delay sweep failed.");
        }
    }

    private async Task TransitionOneAsync(IMediator mediator, Guid processId, Guid tenantId, CancellationToken cancellationToken)
    {
        try
        {
            // The eligible-process query above deliberately reads across every tenant
            // (IgnoreQueryFilters), but the transition command loads the process through
            // the normal, tenant-filtered repository — without this override every
            // lookup would come back "not found" regardless of which tenant it belongs to.
            using var _ = AmbientTenantOverride.Begin(TenantId.From(tenantId));

            var result = await mediator.Send(new TransitionCandidateStageCommand(processId), cancellationToken);
            if (result.IsFailure)
                _logger.LogWarning("Could not auto-advance candidate process {ProcessId}: {Error}", processId, result.Error.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-advance candidate process {ProcessId}.", processId);
        }
    }

    private static readonly PipelineStatus[] TerminalStatuses =
        [PipelineStatus.Accepted, PipelineStatus.Rejected, PipelineStatus.Eliminated, PipelineStatus.TimedOut];
}
