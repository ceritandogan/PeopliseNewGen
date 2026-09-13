using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Entities;

/// <summary>The computed outcome of a completed <see cref="Aggregates.Case"/> — one <see cref="CompetencyResult"/> per assessed competency.</summary>
public sealed class CaseResult : BaseEntity<Guid>
{
    private readonly List<CompetencyResult> _competencyResults = [];

    public DateTimeOffset ComputedAt { get; private set; }
    public IReadOnlyCollection<CompetencyResult> CompetencyResults => _competencyResults.AsReadOnly();

    private CaseResult()
    {
        // Reserved for EF Core materialization.
    }

    public CaseResult(Guid id, DateTimeOffset computedAt) : base(id)
    {
        ComputedAt = computedAt;
    }

    public void AddCompetencyResult(CompetencyResult result) => _competencyResults.Add(result);
}
