using Peoplise.Modules.HrBot.Domain.Entities;
using Peoplise.Modules.HrBot.Domain.ValueObjects;

namespace Peoplise.Modules.HrBot.Domain.Services;

public enum ConversationOutcome
{
    /// <summary>No route's condition matched — the conversation stays where it is.</summary>
    NoMatch,
    MoveToStep,
    SwitchFlow,
    End,
}

public sealed record ConversationDecision(ConversationOutcome Outcome, Guid? TargetFlowId, Guid? TargetStepId);

/// <summary>
/// The flow engine's core decision logic: given the step the conversation is currently
/// on and the candidate's latest response, decide what happens next. Stateless and pure
/// by design — same shape as ATS's <c>Domain.Services.WorkflowEngine</c> — it never
/// touches a <c>Conversation</c> directly; applying the decision (which raises domain
/// events) is <c>ProcessUserResponseCommandHandler</c>'s job.
/// </summary>
public static class ConversationProcessor
{
    public static ConversationDecision Evaluate(Step currentStep, string? candidateResponse)
    {
        if (currentStep.IsFinalStep)
            return new ConversationDecision(ConversationOutcome.End, null, null);

        foreach (var route in currentStep.Routes)
        {
            if (!MatchesCondition(route, candidateResponse))
                continue;

            return route.RouteType switch
            {
                StepRouteType.SwitchFlow => new ConversationDecision(ConversationOutcome.SwitchFlow, route.TargetFlowId, route.TargetStepId),
                StepRouteType.EndConversation => new ConversationDecision(ConversationOutcome.End, null, null),
                _ => new ConversationDecision(ConversationOutcome.MoveToStep, null, route.TargetStepId),
            };
        }

        return new ConversationDecision(ConversationOutcome.NoMatch, null, null);
    }

    private static bool MatchesCondition(StepRoute route, string? response)
    {
        var text = response ?? string.Empty;
        var keywords = route.Keywords;

        return route.ConditionType switch
        {
            ConditionType.NoCondition => true,
            ConditionType.HasAnyKeywords => keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)),
            ConditionType.HasAllKeywords => keywords.Count > 0 && keywords.All(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)),
            ConditionType.DoesNotContainKeywords => keywords.Count > 0 && !keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)),
            ConditionType.HasOnlyKeyword => keywords.Count == 1 && string.Equals(text.Trim(), keywords.First(), StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }
}
