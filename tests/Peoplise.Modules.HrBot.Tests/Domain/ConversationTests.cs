using FluentAssertions;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.Events;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Domain;

public class ConversationTests
{
    private static Conversation CreateConversation() =>
        Conversation.Start(
            BotProjectId.New(), Guid.NewGuid(), ConversationInterface.WebChat,
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public void Start_raises_a_ConversationStartedEvent_and_is_InProgress()
    {
        var conversation = CreateConversation();

        conversation.Status.Should().Be(ConversationStatus.InProgress);
        conversation.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ConversationStartedEvent>();
    }

    [Fact]
    public void SetVariable_replaces_an_existing_value_for_the_same_key()
    {
        var conversation = CreateConversation();

        conversation.SetVariable("salary_expectation", "80000", DateTimeOffset.UtcNow);
        conversation.SetVariable("salary_expectation", "85000", DateTimeOffset.UtcNow);

        conversation.Variables.Should().ContainSingle(v => v.Key == "salary_expectation" && v.Value == "85000");
    }

    [Fact]
    public void Complete_raises_a_ConversationCompletedEvent_and_closes_the_conversation()
    {
        var conversation = CreateConversation();

        var result = conversation.Complete(DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        conversation.Status.Should().Be(ConversationStatus.Completed);
        conversation.IsOpen.Should().BeFalse();
        conversation.DomainEvents.Should().Contain(e => e is ConversationCompletedEvent);
    }

    [Fact]
    public void ScreenOut_raises_a_CandidateScreenedOutEvent()
    {
        var conversation = CreateConversation();

        var result = conversation.ScreenOut("Did not meet the minimum experience bar.", DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        conversation.Status.Should().Be(ConversationStatus.ScreenedOut);
        conversation.DomainEvents.Should().Contain(e => e is CandidateScreenedOutEvent);
    }

    [Fact]
    public void A_closed_conversation_rejects_further_steps()
    {
        var conversation = CreateConversation();
        conversation.Complete(DateTimeOffset.UtcNow);

        var result = conversation.RecordStep(Guid.NewGuid(), "too late", DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conversation.AlreadyClosed");
    }
}
