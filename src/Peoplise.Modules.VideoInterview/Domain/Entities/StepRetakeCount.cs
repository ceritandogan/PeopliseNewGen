using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Entities;

/// <summary>How many retakes a candidate has used at a given video step, enforced against the project's retake policy.</summary>
public sealed class StepRetakeCount : BaseEntity<Guid>
{
    public Guid StepId { get; private set; }
    public int RetakesUsed { get; private set; }

    private StepRetakeCount()
    {
        // Reserved for EF Core materialization.
    }

    public StepRetakeCount(Guid id, Guid stepId) : base(id)
    {
        StepId = stepId;
        RetakesUsed = 0;
    }

    public void Increment() => RetakesUsed++;
}
