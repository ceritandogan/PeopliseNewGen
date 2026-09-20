using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.HrBot.Domain.Entities;

/// <summary>One step-by-step entry in a conversation's transcript, for HR review.</summary>
public sealed class ConversationLog : BaseEntity<Guid>
{
    public Guid StepId { get; private set; }
    public string? CandidateResponse { get; private set; }
    public DateTimeOffset LoggedAt { get; private set; }

    private ConversationLog()
    {
        // Reserved for EF Core materialization.
    }

    public ConversationLog(Guid id, Guid stepId, string? candidateResponse, DateTimeOffset loggedAt) : base(id)
    {
        StepId = stepId;
        CandidateResponse = candidateResponse;
        LoggedAt = loggedAt;
    }

    /// <summary>KVKK anonymization: the candidate's own words are personal data — StepId/LoggedAt stay, for the transcript's shape without its content.</summary>
    public void ClearResponse() => CandidateResponse = null;
}
