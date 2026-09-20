using Peoplise.Modules.HrBot.Domain.ValueObjects;

namespace Peoplise.Modules.HrBot.Application.Common;

/// <summary>
/// The KVKK retention-expiry eligibility rule for HrBot's `Conversation`, mirroring
/// VideoInterview's `CaseRetentionEligibility` — duplicated rather than shared, same
/// reasoning as that one: small and cheap to duplicate once, not worth a premature
/// cross-module abstraction. A `Completed`/`ScreenedOut`/`TimedOut` conversation expires
/// `retentionPeriodDays` after it finished; a conversation still `InProgress` (abandoned
/// mid-chat, never completed) expires that many days after it started.
/// </summary>
public static class ConversationRetentionEligibility
{
    private static readonly ConversationStatus[] EligibleStatuses =
        [ConversationStatus.InProgress, ConversationStatus.Completed, ConversationStatus.ScreenedOut, ConversationStatus.TimedOut];

    public static bool IsEligible(
        ConversationStatus status, DateTimeOffset startedAt, DateTimeOffset? completedAt, int retentionPeriodDays, DateTimeOffset now)
    {
        if (!EligibleStatuses.Contains(status))
            return false;

        var anchor = completedAt ?? startedAt;
        return now > anchor.AddDays(retentionPeriodDays);
    }
}
