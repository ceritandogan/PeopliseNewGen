using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.ATS.Domain.Entities;

/// <summary>
/// Records that a <c>CandidateProcess</c> finished a given stage, and when — the "when"
/// is what makes the "Z gün sonra aktif et" (activate after a delay) workflow rule
/// computable; a plain completed-stage-ids list can't answer "how many days ago".
/// </summary>
public sealed class CompletedStage : BaseEntity<Guid>
{
    public Guid StageId { get; private set; }
    public DateTimeOffset CompletedAt { get; private set; }

    private CompletedStage()
    {
        // Reserved for EF Core materialization.
    }

    public CompletedStage(Guid id, Guid stageId, DateTimeOffset completedAt) : base(id)
    {
        StageId = stageId;
        CompletedAt = completedAt;
    }
}
