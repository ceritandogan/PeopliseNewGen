using FluentAssertions;
using Peoplise.Modules.ATS.Domain.Entities;
using Peoplise.Modules.ATS.Domain.Services;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Xunit;

namespace Peoplise.Modules.ATS.Tests.Domain;

public class WorkflowEngineTests
{
    private static Stage StageWithRules(params StageRule[] rules)
    {
        var stage = new Stage(Guid.NewGuid(), "Screening", StageType.ScreeningTest, order: 0);
        foreach (var rule in rules)
            stage.AddRule(rule);
        return stage;
    }

    [Fact]
    public void Waits_when_no_rule_fires()
    {
        var stage = StageWithRules(StageRule.AdvanceIfScoreAtLeast(70));

        var decision = WorkflowEngine.Evaluate(stage, consensusScore: null, daysSincePreviousStageCompleted: 0);

        decision.Should().Be(WorkflowDecision.Wait);
    }

    [Fact]
    public void Advances_when_the_score_meets_the_advancement_threshold()
    {
        var stage = StageWithRules(StageRule.AdvanceIfScoreAtLeast(70));

        var decision = WorkflowEngine.Evaluate(stage, consensusScore: 75m, daysSincePreviousStageCompleted: 0);

        decision.Should().Be(WorkflowDecision.Advance);
    }

    [Fact]
    public void Does_not_advance_when_the_score_is_below_the_advancement_threshold()
    {
        var stage = StageWithRules(StageRule.AdvanceIfScoreAtLeast(70));

        var decision = WorkflowEngine.Evaluate(stage, consensusScore: 65m, daysSincePreviousStageCompleted: 0);

        decision.Should().Be(WorkflowDecision.Wait);
    }

    [Fact]
    public void Eliminates_when_the_score_is_below_the_elimination_threshold()
    {
        var stage = StageWithRules(StageRule.EliminateIfScoreBelow(40));

        var decision = WorkflowEngine.Evaluate(stage, consensusScore: 30m, daysSincePreviousStageCompleted: 0);

        decision.Should().Be(WorkflowDecision.Eliminate);
    }

    [Fact]
    public void Elimination_wins_over_advancement_when_both_thresholds_are_crossed_the_wrong_way()
    {
        // A score that clears the (low) elimination floor's *complement* but still
        // falls under it must eliminate even if some other rule would otherwise advance —
        // here, a very low score is below both an elimination floor and (trivially)
        // fails to meet a much higher advancement bar, but the key case is elimination
        // taking priority when it fires at all.
        var stage = StageWithRules(
            StageRule.EliminateIfScoreBelow(40),
            StageRule.AdvanceIfScoreAtLeast(0));

        var decision = WorkflowEngine.Evaluate(stage, consensusScore: 20m, daysSincePreviousStageCompleted: 0);

        decision.Should().Be(WorkflowDecision.Eliminate);
    }

    [Fact]
    public void Advances_after_the_configured_delay_has_elapsed_with_no_score_yet()
    {
        var stage = StageWithRules(StageRule.ActivateAfterDelay(3));

        var decision = WorkflowEngine.Evaluate(stage, consensusScore: null, daysSincePreviousStageCompleted: 3);

        decision.Should().Be(WorkflowDecision.Advance);
    }

    [Fact]
    public void Waits_when_the_configured_delay_has_not_yet_elapsed()
    {
        var stage = StageWithRules(StageRule.ActivateAfterDelay(3));

        var decision = WorkflowEngine.Evaluate(stage, consensusScore: null, daysSincePreviousStageCompleted: 2);

        decision.Should().Be(WorkflowDecision.Wait);
    }

    [Fact]
    public void A_stage_with_no_rules_always_waits()
    {
        var stage = StageWithRules();

        var decision = WorkflowEngine.Evaluate(stage, consensusScore: 100m, daysSincePreviousStageCompleted: 999);

        decision.Should().Be(WorkflowDecision.Wait);
    }
}
