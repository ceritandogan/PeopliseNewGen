using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Entities;

/// <summary>
/// The next step after this one. Deliberately minimal compared to HrBot's StepRoute —
/// case-bot flows are a fixed assessment sequence, not a branching conversation, so
/// there's no condition-type system here, just "what comes next."
/// </summary>
public sealed class StepRoute : BaseEntity<Guid>
{
    public Guid TargetStepId { get; private set; }

    private StepRoute()
    {
        // Reserved for EF Core materialization.
    }

    public StepRoute(Guid id, Guid targetStepId) : base(id)
    {
        TargetStepId = targetStepId;
    }
}
