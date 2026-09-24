using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Peoplise.Modules.ATS.Application.Candidates.Commands;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.Entities;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Api.Tests;

/// <summary>
/// Proves the fully-automatic StageRule trigger wiring genuinely works against the real,
/// composed application — not the ATS module in isolation with a mocked repository (see
/// Peoplise.Modules.ATS.Tests for that level). Resolves IMediator from the real DI
/// container and sends SubmitEvaluationCommand directly (no HTTP endpoint exists for it —
/// deliberately out of scope, see the StageRule-authoring session's own notes), which
/// must raise EvaluationSubmittedEvent, which the real, DI-registered
/// EvaluationSubmittedEventHandler must catch and turn into a real
/// TransitionCandidateStageCommand — proving the whole chain is actually wired, not just
/// individually correct.
///
/// There's no HTTP request here, so <c>HttpContextTenantContext</c> has no JWT tenant
/// claim to resolve — and <c>ApiTestFactory</c>'s in-memory AppDbContext registration
/// doesn't chain the production TenantInterceptor either, so nothing stamps
/// TenantId automatically. Both are worked around exactly like
/// WorkflowStageDelaySweepBackgroundService does for the same reason: an explicit
/// AmbientTenantOverride for reads, and the tenant set by hand on each seeded aggregate
/// so what's saved actually matches what the query filter looks for on the way back out.
/// </summary>
public class EvaluationTriggersStageTransitionTests
{
    [Fact]
    public async Task Submitting_an_evaluation_that_clears_the_stage_threshold_auto_advances_the_candidate()
    {
        var tenantId = TenantId.New();
        using var tenantScope = AmbientTenantOverride.Begin(tenantId);

        await using var factory = new ApiTestFactory();
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var workflows = services.GetRequiredService<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        var processes = services.GetRequiredService<IRepository<CandidateProcess, CandidateProcessId>>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var mediator = services.GetRequiredService<IMediator>();

        var workflow = WorkflowDefinition.Create("Standard Flow");
        workflow.TenantId = tenantId.Value;
        var firstStage = workflow.AddStage("Screening Test", StageType.ScreeningTest, order: 0).Value;
        var secondStage = workflow.AddStage("Live Interview", StageType.LiveInterview, order: 1).Value;
        firstStage.AddRule(StageRule.AdvanceIfScoreAtLeast(70m));
        await workflows.AddAsync(workflow);

        var process = CandidateProcess.Submit(
            CandidateId.New(), PositionId.New(), workflow.Id,
            "Ada Lovelace", "ada@example.com", null, null, firstStage.Id, DateTimeOffset.UtcNow);
        process.TenantId = tenantId.Value;
        await processes.AddAsync(process);
        await unitOfWork.SaveChangesAsync();

        var result = await mediator.Send(new SubmitEvaluationCommand(process.Id.Value, "reviewer-1", 85, "Strong technical answers."));

        result.IsSuccess.Should().BeTrue();

        // Reload from a fresh scope — proves the transition was actually persisted by
        // the event handler's own SaveChangesAsync, not just mutated in the in-memory
        // instance this test already holds a reference to.
        using var verificationScope = factory.Services.CreateScope();
        var verificationProcesses = verificationScope.ServiceProvider.GetRequiredService<IRepository<CandidateProcess, CandidateProcessId>>();
        var reloaded = await verificationProcesses.GetByIdAsync(process.Id);

        reloaded.Should().NotBeNull();
        reloaded!.CurrentStageId.Should().Be(secondStage.Id, "a score at/above the stage's AdvanceIfScoreAtLeast threshold should auto-advance the candidate");
    }

    [Fact]
    public async Task Submitting_an_evaluation_below_the_stage_threshold_leaves_the_candidate_in_place()
    {
        var tenantId = TenantId.New();
        using var tenantScope = AmbientTenantOverride.Begin(tenantId);

        await using var factory = new ApiTestFactory();
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var workflows = services.GetRequiredService<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        var processes = services.GetRequiredService<IRepository<CandidateProcess, CandidateProcessId>>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var mediator = services.GetRequiredService<IMediator>();

        var workflow = WorkflowDefinition.Create("Standard Flow");
        workflow.TenantId = tenantId.Value;
        var firstStage = workflow.AddStage("Screening Test", StageType.ScreeningTest, order: 0).Value;
        workflow.AddStage("Live Interview", StageType.LiveInterview, order: 1);
        firstStage.AddRule(StageRule.AdvanceIfScoreAtLeast(70m));
        await workflows.AddAsync(workflow);

        var process = CandidateProcess.Submit(
            CandidateId.New(), PositionId.New(), workflow.Id,
            "Grace Hopper", "grace@example.com", null, null, firstStage.Id, DateTimeOffset.UtcNow);
        process.TenantId = tenantId.Value;
        await processes.AddAsync(process);
        await unitOfWork.SaveChangesAsync();

        var result = await mediator.Send(new SubmitEvaluationCommand(process.Id.Value, "reviewer-1", 40, "Needs more depth."));

        result.IsSuccess.Should().BeTrue();

        using var verificationScope = factory.Services.CreateScope();
        var verificationProcesses = verificationScope.ServiceProvider.GetRequiredService<IRepository<CandidateProcess, CandidateProcessId>>();
        var reloaded = await verificationProcesses.GetByIdAsync(process.Id);

        reloaded!.CurrentStageId.Should().Be(firstStage.Id, "a score below the threshold should leave the candidate on the same stage, waiting");
    }
}
