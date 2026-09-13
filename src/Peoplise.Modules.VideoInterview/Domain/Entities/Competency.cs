using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Entities;

/// <summary>E.g. "Analitik Düşünme", "Liderlik", "Problem Çözme" — one axis of the assessment rubric.</summary>
public sealed class Competency : BaseEntity<Guid>
{
    private readonly List<CompetencyLevel> _levels = [];
    private readonly List<BehavioralIndicator> _indicators = [];

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public IReadOnlyCollection<CompetencyLevel> Levels => _levels.AsReadOnly();
    public IReadOnlyCollection<BehavioralIndicator> Indicators => _indicators.AsReadOnly();

    private Competency()
    {
        // Reserved for EF Core materialization.
    }

    public Competency(Guid id, string name, string? description) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A competency must have a name.", nameof(name));

        Name = name;
        Description = description;
    }

    public void AddLevel(CompetencyLevel level) => _levels.Add(level);

    public void AddIndicator(BehavioralIndicator indicator) => _indicators.Add(indicator);
}
