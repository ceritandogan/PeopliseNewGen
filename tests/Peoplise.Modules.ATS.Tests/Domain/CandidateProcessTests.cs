using FluentAssertions;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.Events;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Xunit;

namespace Peoplise.Modules.ATS.Tests.Domain;

public class CandidateProcessTests
{
    private static readonly Guid FirstStageId = Guid.NewGuid();
    private static readonly Guid SecondStageId = Guid.NewGuid();

    private static CandidateProcess CreateProcess() =>
        CandidateProcess.Submit(
            CandidateId.New(), PositionId.New(), WorkflowDefinitionId.New(),
            "Ada Lovelace", "ada@example.com", "+90 555 000 0000", "https://resume.example/ada",
            FirstStageId, DateTimeOffset.UtcNow);

    [Fact]
    public void Submit_raises_a_CandidateAppliedEvent()
    {
        var process = CreateProcess();

        process.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CandidateAppliedEvent>();
    }

    [Fact]
    public void MoveToNextStage_fails_when_the_current_stage_has_not_been_completed()
    {
        var process = CreateProcess();

        var result = process.MoveToNextStage(SecondStageId, DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CandidateProcess.PreviousStageNotCompleted");
        process.CurrentStageId.Should().Be(FirstStageId, "the failed transition must not have moved the process");
    }

    [Fact]
    public void MoveToNextStage_succeeds_once_the_current_stage_is_completed()
    {
        var process = CreateProcess();
        process.CompleteCurrentStage(DateTimeOffset.UtcNow);

        var result = process.MoveToNextStage(SecondStageId, DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        process.CurrentStageId.Should().Be(SecondStageId);
        process.DomainEvents.Should().ContainSingle(e => e is CandidateMovedToNextStageEvent);
    }

    [Fact]
    public void CompleteCurrentStage_raises_a_StageCompletedEvent()
    {
        var process = CreateProcess();

        var completedAt = DateTimeOffset.UtcNow;
        var result = process.CompleteCurrentStage(completedAt);

        result.IsSuccess.Should().BeTrue();
        process.CompletedStages.Should().ContainSingle(cs => cs.StageId == FirstStageId);
        process.GetCompletedAt(FirstStageId).Should().Be(completedAt);
        process.DomainEvents.Should().Contain(e => e is StageCompletedEvent);
    }

    [Fact]
    public void CalculateConsensusScore_fails_when_no_evaluations_exist_for_the_stage()
    {
        var process = CreateProcess();

        var result = process.CalculateConsensusScore(FirstStageId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CandidateProcess.NoEvaluationsYet");
    }

    [Fact]
    public void CalculateConsensusScore_averages_every_evaluation_submitted_for_the_stage()
    {
        var process = CreateProcess();
        process.SubmitEvaluation("evaluator-1", EvaluationScore.From(80), null, DateTimeOffset.UtcNow);
        process.SubmitEvaluation("evaluator-2", EvaluationScore.From(60), null, DateTimeOffset.UtcNow);

        var result = process.CalculateConsensusScore(FirstStageId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(70);
    }

    [Fact]
    public void SubmitEvaluation_rejects_a_second_evaluation_from_the_same_evaluator_for_the_same_stage()
    {
        var process = CreateProcess();
        process.SubmitEvaluation("evaluator-1", EvaluationScore.From(80), null, DateTimeOffset.UtcNow);

        var result = process.SubmitEvaluation("evaluator-1", EvaluationScore.From(90), null, DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CandidateProcess.DuplicateEvaluation");
        process.Evaluations.Should().ContainSingle();
    }

    [Fact]
    public void Eliminate_raises_a_CandidateEliminatedEvent_and_sets_a_terminal_status()
    {
        var process = CreateProcess();

        var result = process.Eliminate("Did not meet the bar in the screening test.");

        result.IsSuccess.Should().BeTrue();
        process.Status.Should().Be(PipelineStatus.Eliminated);
        process.IsInTerminalState.Should().BeTrue();
        process.DomainEvents.Should().Contain(e => e is CandidateEliminatedEvent);
    }

    [Fact]
    public void A_process_already_in_a_terminal_state_cannot_be_eliminated_again()
    {
        var process = CreateProcess();
        process.Eliminate("First reason");

        var result = process.Eliminate("Second reason");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CandidateProcess.AlreadyClosed");
    }

    [Fact]
    public void A_process_already_in_a_terminal_state_cannot_move_stages()
    {
        var process = CreateProcess();
        process.Eliminate("Did not meet the bar.");

        var result = process.MoveToNextStage(SecondStageId, DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CandidateProcess.AlreadyClosed");
    }
}
