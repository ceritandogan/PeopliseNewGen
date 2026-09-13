using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Entities;

/// <summary>The computed weighted-average score for one competency, part of a <see cref="CaseResult"/>.</summary>
public sealed class CompetencyResult : BaseEntity<Guid>
{
    public Guid CompetencyId { get; private set; }
    public decimal Score { get; private set; }

    private CompetencyResult()
    {
        // Reserved for EF Core materialization.
    }

    public CompetencyResult(Guid id, Guid competencyId, decimal score) : base(id)
    {
        CompetencyId = competencyId;
        Score = score;
    }
}
