using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Entities;

/// <summary>One step in a <see cref="Flow"/>.</summary>
public sealed class Step : BaseEntity<Guid>
{
    private readonly List<StepRoute> _routes = [];
    private readonly List<Guid> _relatedCompetencyIds = [];

    public int Order { get; private set; }
    public StepType Type { get; private set; }
    public string Content { get; private set; } = string.Empty;

    /// <summary>"Düşünme Süresi" — countdown shown before recording starts. <see cref="StepType.RecordVideoAnswer"/> / <see cref="StepType.SoftwareDevelopmentQuestion"/>.</summary>
    public int? PreparationTimeSeconds { get; private set; }

    /// <summary>"Cevaplama Süresi" — max recording length. <see cref="StepType.RecordVideoAnswer"/>.</summary>
    public int? RecordingTimeSeconds { get; private set; }

    public IReadOnlyCollection<Guid> RelatedCompetencyIds => _relatedCompetencyIds.AsReadOnly();
    public IReadOnlyCollection<StepRoute> Routes => _routes.AsReadOnly();

    private Step()
    {
        // Reserved for EF Core materialization.
    }

    public Step(
        Guid id, int order, StepType type, string content,
        int? preparationTimeSeconds = null, int? recordingTimeSeconds = null,
        IEnumerable<Guid>? relatedCompetencyIds = null)
        : base(id)
    {
        if (order < 0)
            throw new ArgumentOutOfRangeException(nameof(order), order, "A step's order cannot be negative.");

        Order = order;
        Type = type;
        Content = content;
        PreparationTimeSeconds = preparationTimeSeconds;
        RecordingTimeSeconds = recordingTimeSeconds;
        _relatedCompetencyIds.AddRange(relatedCompetencyIds ?? []);
    }

    public void AddRoute(StepRoute route) => _routes.Add(route);
}
