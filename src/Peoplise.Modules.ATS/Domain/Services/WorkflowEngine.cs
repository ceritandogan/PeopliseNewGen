using Peoplise.Modules.ATS.Domain.Entities;
using Peoplise.Modules.ATS.Domain.ValueObjects;

namespace Peoplise.Modules.ATS.Domain.Services;

/// <summary>What the current stage's rules say should happen next.</summary>
public enum WorkflowDecision
{
    /// <summary>No rule fired yet — wait for more input (another evaluation, more elapsed time).</summary>
    Wait,
    Advance,
    Eliminate,
}

/// <summary>
/// Evaluates a <see cref="Stage"/>'s <see cref="StageRule"/>s against the candidate's
/// current consensus score and elapsed time, and decides whether the workflow should
/// auto-advance, auto-eliminate, or keep waiting. Stateless and pure by design: it reads
/// a <see cref="Stage"/> (from a <c>WorkflowDefinition</c>) and scalar inputs, and
/// produces a decision — it never touches a <c>CandidateProcess</c> directly, since
/// applying that decision (which raises domain events) is the aggregate's job, not this
/// service's. See <c>TransitionCandidateStageCommandHandler</c> for how the two connect.
/// </summary>
public static class WorkflowEngine
{
    /// <summary>
    /// Elimination rules are checked before advancement rules: a candidate who both
    /// clears the elimination floor and meets the advancement bar should still be
    /// eliminated if their score is below an elimination threshold — the elimination
    /// rule exists specifically to catch that case, so it must win.
    /// </summary>
    public static WorkflowDecision Evaluate(Stage stage, decimal? consensusScore, int daysSincePreviousStageCompleted)
    {
        var eliminationRules = stage.Rules.Where(r => r.Type == StageRuleType.EliminateIfScoreBelow);
        foreach (var rule in eliminationRules)
        {
            if (consensusScore is not null && consensusScore.Value < rule.Threshold!.Value)
                return WorkflowDecision.Eliminate;
        }

        var advancementRules = stage.Rules.Where(r =>
            r.Type is StageRuleType.AdvanceIfScoreAtLeast or StageRuleType.ActivateAfterDelay);

        foreach (var rule in advancementRules)
        {
            switch (rule.Type)
            {
                case StageRuleType.AdvanceIfScoreAtLeast when consensusScore is not null && consensusScore.Value >= rule.Threshold!.Value:
                case StageRuleType.ActivateAfterDelay when daysSincePreviousStageCompleted >= rule.DelayDays!.Value:
                    return WorkflowDecision.Advance;
            }
        }

        return WorkflowDecision.Wait;
    }
}
