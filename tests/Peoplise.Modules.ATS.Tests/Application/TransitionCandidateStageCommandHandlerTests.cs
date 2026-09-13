using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.ATS.Application.Candidates.Commands;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.Entities;
using Peoplise.Modules.ATS.Domain.Events;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.ATS.Tests.Application;

public class TransitionCandidateStageCommandHandlerTests
{
    private static (
        IRepository<CandidateProcess, CandidateProcessId> Processes,
        IRepository<WorkflowDefinition, WorkflowDefinitionId> Workflows,
        IUnitOfWork UnitOfWork,
        TransitionCandidateStageCommandHandler Handler) CreateHandler()
    {
        var processes = Substitute.For<IRepository<CandidateProcess, CandidateProcessId>>();
        var workflows = Substitute.For<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new TransitionCandidateStageCommandHandler(processes, workflows, unitOfWork);
        return (processes, workflows, unitOfWork, handler);
    }

    [Fact]
    public async Task Returns_NotFound_when_the_process_does_not_exist()
    {
        var (processes, _, _, handler) = CreateHandler();
        processes.GetByIdAsync(Arg.Any<CandidateProcessId>(), Arg.Any<CancellationToken>()).Returns((CandidateProcess?)null);

        var result = await handler.Handle(new TransitionCandidateStageCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CandidateProcess.NotFound");
    }

    [Fact]
    public async Task Eliminates_the_candidate_when_the_consensus_score_is_below_the_elimination_threshold()
    {
        var (processes, workflows, unitOfWork, handler) = CreateHandler();

        var workflow = WorkflowDefinition.Create("Standard Flow");
        var stage = workflow.AddStage("Screening Test", StageType.ScreeningTest, order: 0).Value;
        stage.AddRule(StageRule.EliminateIfScoreBelow(40));

        var process = CandidateProcess.Submit(
            CandidateId.New(), PositionId.New(), workflow.Id,
            "Ada Lovelace", "ada@example.com", null, null, stage.Id, DateTimeOffset.UtcNow);
        process.SubmitEvaluation("evaluator-1", EvaluationScore.From(20), null, DateTimeOffset.UtcNow);

        processes.GetByIdAsync(process.Id, Arg.Any<CancellationToken>()).Returns(process);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);

        var result = await handler.Handle(new TransitionCandidateStageCommand(process.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        process.Status.Should().Be(PipelineStatus.Eliminated);
        process.DomainEvents.Should().Contain(e => e is CandidateEliminatedEvent);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Advances_the_candidate_to_the_next_stage_when_the_score_meets_the_advancement_threshold()
    {
        var (processes, workflows, unitOfWork, handler) = CreateHandler();

        var workflow = WorkflowDefinition.Create("Standard Flow");
        var firstStage = workflow.AddStage("Screening Test", StageType.ScreeningTest, order: 0).Value;
        firstStage.AddRule(StageRule.AdvanceIfScoreAtLeast(70));
        var secondStage = workflow.AddStage("Live Interview", StageType.LiveInterview, order: 1).Value;

        var process = CandidateProcess.Submit(
            CandidateId.New(), PositionId.New(), workflow.Id,
            "Ada Lovelace", "ada@example.com", null, null, firstStage.Id, DateTimeOffset.UtcNow);
        process.SubmitEvaluation("evaluator-1", EvaluationScore.From(90), null, DateTimeOffset.UtcNow);

        processes.GetByIdAsync(process.Id, Arg.Any<CancellationToken>()).Returns(process);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);

        var result = await handler.Handle(new TransitionCandidateStageCommand(process.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        process.CurrentStageId.Should().Be(secondStage.Id);
        process.CompletedStages.Should().ContainSingle(cs => cs.StageId == firstStage.Id);
        process.DomainEvents.Should().Contain(e => e is StageCompletedEvent);
        process.DomainEvents.Should().Contain(e => e is CandidateMovedToNextStageEvent);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Leaves_the_candidate_in_place_when_no_rule_fires_yet()
    {
        var (processes, workflows, unitOfWork, handler) = CreateHandler();

        var workflow = WorkflowDefinition.Create("Standard Flow");
        var stage = workflow.AddStage("Screening Test", StageType.ScreeningTest, order: 0).Value;
        stage.AddRule(StageRule.AdvanceIfScoreAtLeast(70));

        var process = CandidateProcess.Submit(
            CandidateId.New(), PositionId.New(), workflow.Id,
            "Ada Lovelace", "ada@example.com", null, null, stage.Id, DateTimeOffset.UtcNow);
        process.SubmitEvaluation("evaluator-1", EvaluationScore.From(50), null, DateTimeOffset.UtcNow);

        processes.GetByIdAsync(process.Id, Arg.Any<CancellationToken>()).Returns(process);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);

        var result = await handler.Handle(new TransitionCandidateStageCommand(process.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        process.CurrentStageId.Should().Be(stage.Id, "no rule fired, so the candidate must stay put");
        process.Status.Should().Be(PipelineStatus.NewApplication);
    }
}
