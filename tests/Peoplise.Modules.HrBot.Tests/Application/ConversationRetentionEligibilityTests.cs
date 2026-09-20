using FluentAssertions;
using Peoplise.Modules.HrBot.Application.Common;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Application;

public class ConversationRetentionEligibilityTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_completed_conversation_is_eligible_once_the_retention_period_has_elapsed_since_completion()
    {
        var completedAt = Now.AddDays(-31);

        var eligible = ConversationRetentionEligibility.IsEligible(
            ConversationStatus.Completed, startedAt: completedAt.AddHours(-1), completedAt, retentionPeriodDays: 30, Now);

        eligible.Should().BeTrue();
    }

    [Fact]
    public void A_completed_conversation_is_not_yet_eligible_within_the_retention_period()
    {
        var completedAt = Now.AddDays(-29);

        var eligible = ConversationRetentionEligibility.IsEligible(
            ConversationStatus.Completed, startedAt: completedAt.AddHours(-1), completedAt, retentionPeriodDays: 30, Now);

        eligible.Should().BeFalse();
    }

    [Fact]
    public void A_screened_out_conversation_is_anchored_on_CompletedAt_like_a_completed_one()
    {
        var completedAt = Now.AddDays(-31);

        var eligible = ConversationRetentionEligibility.IsEligible(
            ConversationStatus.ScreenedOut, startedAt: completedAt.AddHours(-1), completedAt, retentionPeriodDays: 30, Now);

        eligible.Should().BeTrue();
    }

    [Fact]
    public void An_abandoned_in_progress_conversation_is_anchored_on_StartedAt_not_CompletedAt()
    {
        var startedAt = Now.AddDays(-31);

        var eligible = ConversationRetentionEligibility.IsEligible(
            ConversationStatus.InProgress, startedAt, completedAt: null, retentionPeriodDays: 30, Now);

        eligible.Should().BeTrue("a candidate who never finished chatting still has their data age out");
    }

    [Fact]
    public void A_conversation_that_already_withdrew_consent_is_never_eligible_again()
    {
        var eligible = ConversationRetentionEligibility.IsEligible(
            ConversationStatus.ConsentWithdrawn, startedAt: Now.AddYears(-1), completedAt: Now.AddYears(-1), retentionPeriodDays: 1, Now);

        eligible.Should().BeFalse();
    }

    [Fact]
    public void A_conversation_already_expired_is_never_eligible_again()
    {
        var eligible = ConversationRetentionEligibility.IsEligible(
            ConversationStatus.RetentionExpired, startedAt: Now.AddYears(-1), completedAt: Now.AddYears(-1), retentionPeriodDays: 1, Now);

        eligible.Should().BeFalse();
    }
}
