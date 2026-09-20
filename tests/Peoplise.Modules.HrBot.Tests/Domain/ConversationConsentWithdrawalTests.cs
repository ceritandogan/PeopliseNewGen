using FluentAssertions;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.Events;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Domain;

public class ConversationConsentWithdrawalTests
{
    private static Conversation CreateConversation() =>
        Conversation.Start(
            BotProjectId.New(), Guid.NewGuid(), ConversationInterface.WebChat,
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public void WithdrawConsent_clears_every_logged_response_and_captured_variable()
    {
        var conversation = CreateConversation();
        conversation.RecordStep(Guid.NewGuid(), "I currently make 80000.", DateTimeOffset.UtcNow);
        conversation.SetVariable("salary_expectation", "85000", DateTimeOffset.UtcNow);

        var result = conversation.WithdrawConsent("Candidate requested deletion.", DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        conversation.Logs.Should().OnlyContain(l => l.CandidateResponse == null);
        conversation.Variables.Should().OnlyContain(v => v.Value == string.Empty);
    }

    [Fact]
    public void WithdrawConsent_severs_the_candidate_identity()
    {
        var conversation = CreateConversation();

        conversation.WithdrawConsent("Candidate requested deletion.", DateTimeOffset.UtcNow);

        conversation.CandidateId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void WithdrawConsent_raises_a_ConsentWithdrawnEvent_and_closes_the_conversation()
    {
        var conversation = CreateConversation();

        var result = conversation.WithdrawConsent("No longer interested.", DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        conversation.Status.Should().Be(ConversationStatus.ConsentWithdrawn);
        conversation.IsOpen.Should().BeFalse();
        conversation.DomainEvents.Should().Contain(e => e is ConsentWithdrawnEvent);
    }

    [Fact]
    public void A_closed_conversation_cannot_withdraw_consent_twice()
    {
        var conversation = CreateConversation();
        conversation.WithdrawConsent("First.", DateTimeOffset.UtcNow);

        var result = conversation.WithdrawConsent("Second.", DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conversation.AlreadyClosed");
    }

    [Fact]
    public void ExpireRetention_runs_on_an_already_completed_conversation()
    {
        var conversation = CreateConversation();
        conversation.SetVariable("location", "Istanbul", DateTimeOffset.UtcNow);
        conversation.Complete(DateTimeOffset.UtcNow);

        var result = conversation.ExpireRetention(DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue("a retention sweep's main target is data that aged out after the conversation finished");
        conversation.Status.Should().Be(ConversationStatus.RetentionExpired);
        conversation.CandidateId.Should().Be(Guid.Empty);
        conversation.Variables.Should().OnlyContain(v => v.Value == string.Empty);
    }

    [Fact]
    public void ExpireRetention_preserves_the_original_completion_date_of_an_already_completed_conversation()
    {
        var conversation = CreateConversation();
        var completedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        conversation.Complete(completedAt);

        conversation.ExpireRetention(completedAt.AddDays(30));

        conversation.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void ExpireRetention_sets_CompletedAt_for_a_conversation_that_was_never_completed()
    {
        var conversation = CreateConversation();
        var expiredAt = DateTimeOffset.UtcNow;

        conversation.ExpireRetention(expiredAt);

        conversation.CompletedAt.Should().Be(expiredAt);
    }

    [Fact]
    public void ExpireRetention_cannot_run_twice()
    {
        var conversation = CreateConversation();
        conversation.ExpireRetention(DateTimeOffset.UtcNow);

        var result = conversation.ExpireRetention(DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conversation.AlreadyAnonymized");
    }

    [Fact]
    public void ExpireRetention_cannot_run_on_a_conversation_that_already_withdrew_consent()
    {
        var conversation = CreateConversation();
        conversation.WithdrawConsent("Candidate requested deletion.", DateTimeOffset.UtcNow);

        var result = conversation.ExpireRetention(DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conversation.AlreadyAnonymized");
    }
}
