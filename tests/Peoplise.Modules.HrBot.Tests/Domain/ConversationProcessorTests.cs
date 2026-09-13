using FluentAssertions;
using Peoplise.Modules.HrBot.Domain.Entities;
using Peoplise.Modules.HrBot.Domain.Services;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Domain;

public class ConversationProcessorTests
{
    [Fact]
    public void A_final_step_always_ends_the_conversation_regardless_of_routes()
    {
        var step = new Step(Guid.NewGuid(), 0, StepType.SendMessage, "Goodbye", isFinalStep: true);

        var decision = ConversationProcessor.Evaluate(step, "anything");

        decision.Outcome.Should().Be(ConversationOutcome.End);
    }

    [Fact]
    public void A_NoCondition_route_always_matches_and_advances_unconditionally()
    {
        var targetStepId = Guid.NewGuid();
        var step = new Step(Guid.NewGuid(), 0, StepType.SendMessage, "Hi");
        step.AddRoute(StepRoute.ToStep(ConditionType.NoCondition, [], targetStepId));

        var decision = ConversationProcessor.Evaluate(step, "whatever the candidate typed");

        decision.Outcome.Should().Be(ConversationOutcome.MoveToStep);
        decision.TargetStepId.Should().Be(targetStepId);
    }

    [Fact]
    public void HasAnyKeywords_matches_when_the_response_contains_one_of_the_keywords()
    {
        var targetStepId = Guid.NewGuid();
        var step = new Step(Guid.NewGuid(), 0, StepType.WaitResponse, "Experience?");
        step.AddRoute(StepRoute.ToStep(ConditionType.HasAnyKeywords, ["senior", "lead"], targetStepId));

        var decision = ConversationProcessor.Evaluate(step, "I'm a Senior Engineer");

        decision.Outcome.Should().Be(ConversationOutcome.MoveToStep);
        decision.TargetStepId.Should().Be(targetStepId);
    }

    [Fact]
    public void HasAllKeywords_requires_every_keyword_to_be_present()
    {
        var targetStepId = Guid.NewGuid();
        var step = new Step(Guid.NewGuid(), 0, StepType.WaitResponse, "Availability?");
        step.AddRoute(StepRoute.ToStep(ConditionType.HasAllKeywords, ["remote", "full-time"], targetStepId));

        var partialMatch = ConversationProcessor.Evaluate(step, "I want remote work");
        var fullMatch = ConversationProcessor.Evaluate(step, "I want remote, full-time work");

        partialMatch.Outcome.Should().Be(ConversationOutcome.NoMatch);
        fullMatch.Outcome.Should().Be(ConversationOutcome.MoveToStep);
    }

    [Fact]
    public void DoesNotContainKeywords_matches_when_none_of_the_keywords_are_present()
    {
        var targetStepId = Guid.NewGuid();
        var step = new Step(Guid.NewGuid(), 0, StepType.WaitResponse, "Notice period?");
        step.AddRoute(StepRoute.ToStep(ConditionType.DoesNotContainKeywords, ["immediately"], targetStepId));

        var decision = ConversationProcessor.Evaluate(step, "I can start in 2 months");

        decision.Outcome.Should().Be(ConversationOutcome.MoveToStep);
    }

    [Fact]
    public void HasOnlyKeyword_requires_an_exact_match_like_a_quick_reply_button_click()
    {
        var yesStepId = Guid.NewGuid();
        var step = new Step(Guid.NewGuid(), 0, StepType.SendQuickReply, "Remote OK?", quickReplyOptions: ["Evet", "Hayır"]);
        step.AddRoute(StepRoute.ToStep(ConditionType.HasOnlyKeyword, ["Evet"], yesStepId));

        var exactClick = ConversationProcessor.Evaluate(step, "Evet");
        var freeText = ConversationProcessor.Evaluate(step, "Evet, kesinlikle olur");

        exactClick.Outcome.Should().Be(ConversationOutcome.MoveToStep);
        freeText.Outcome.Should().Be(ConversationOutcome.NoMatch, "HasOnlyKeyword must not match free text containing the keyword");
    }

    [Fact]
    public void Branching_to_a_different_flow_returns_SwitchFlow_with_both_ids()
    {
        var targetFlowId = Guid.NewGuid();
        var targetStepId = Guid.NewGuid();
        var step = new Step(Guid.NewGuid(), 0, StepType.SwitchFlow, "");
        step.AddRoute(StepRoute.ToFlow(ConditionType.NoCondition, [], targetFlowId, targetStepId));

        var decision = ConversationProcessor.Evaluate(step, null);

        decision.Outcome.Should().Be(ConversationOutcome.SwitchFlow);
        decision.TargetFlowId.Should().Be(targetFlowId);
        decision.TargetStepId.Should().Be(targetStepId);
    }

    [Fact]
    public void A_route_may_end_the_conversation_without_the_step_itself_being_final()
    {
        var step = new Step(Guid.NewGuid(), 0, StepType.WaitResponse, "Still interested?");
        step.AddRoute(StepRoute.EndConversation(ConditionType.HasOnlyKeyword, ["Hayır"]));

        var decision = ConversationProcessor.Evaluate(step, "Hayır");

        decision.Outcome.Should().Be(ConversationOutcome.End);
    }

    [Fact]
    public void No_matching_route_returns_NoMatch_and_leaves_the_conversation_in_place()
    {
        var step = new Step(Guid.NewGuid(), 0, StepType.WaitResponse, "City?");
        step.AddRoute(StepRoute.ToStep(ConditionType.HasAnyKeywords, ["Istanbul"], Guid.NewGuid()));

        var decision = ConversationProcessor.Evaluate(step, "Ankara");

        decision.Outcome.Should().Be(ConversationOutcome.NoMatch);
    }

    [Fact]
    public void The_first_matching_route_wins_when_several_are_defined()
    {
        var firstTarget = Guid.NewGuid();
        var secondTarget = Guid.NewGuid();
        var step = new Step(Guid.NewGuid(), 0, StepType.WaitResponse, "Years of experience?");
        step.AddRoute(StepRoute.ToStep(ConditionType.HasAnyKeywords, ["5"], firstTarget));
        step.AddRoute(StepRoute.ToStep(ConditionType.NoCondition, [], secondTarget));

        var decision = ConversationProcessor.Evaluate(step, "5 years");

        decision.TargetStepId.Should().Be(firstTarget);
    }
}
