using Peoplise.Modules.VideoInterview.Domain.ValueObjects;

namespace Peoplise.Modules.VideoInterview.Application.Common;

/// <summary>
/// The KVKK retention-expiry eligibility rule (architecture doc, Section G), factored
/// out of the sweep job so it's testable without a database. A
/// <see cref="CaseStatus.Completed"/>/<see cref="CaseStatus.TimedOut"/> case expires
/// <c>retentionPeriodDays</c> after it finished; a case still <see cref="CaseStatus.InProgress"/>
/// (abandoned mid-interview, never completed) expires that many days after it started.
/// </summary>
public static class CaseRetentionEligibility
{
    private static readonly CaseStatus[] EligibleStatuses =
        [CaseStatus.InProgress, CaseStatus.Completed, CaseStatus.TimedOut];

    public static bool IsEligible(
        CaseStatus status, DateTimeOffset startedAt, DateTimeOffset? completedAt, int retentionPeriodDays, DateTimeOffset now)
    {
        if (!EligibleStatuses.Contains(status))
            return false;

        var anchor = completedAt ?? startedAt;
        return now > anchor.AddDays(retentionPeriodDays);
    }
}
