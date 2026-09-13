using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Entities;

/// <summary>
/// One reviewer's (human or AI) independent score against a competency at a given step —
/// "Çoklu değerlendirici: Bağımsız puanlama, sonradan konsensüs tablosu." Several of
/// these, for the same competency, are what <see cref="Case.CalculateCompetencyResult"/>
/// averages into a <see cref="CompetencyResult"/>.
/// </summary>
public sealed class CaseScoring : BaseEntity<Guid>
{
    public string ReviewerId { get; private set; } = string.Empty;
    public Guid StepId { get; private set; }
    public Guid CompetencyId { get; private set; }
    public int Score { get; private set; }

    /// <summary>How much this scoring counts toward the competency's weighted average. Defaults to 1.</summary>
    public decimal Weight { get; private set; }

    public string? Notes { get; private set; }
    public bool IsAiGenerated { get; private set; }
    public DateTimeOffset ScoredAt { get; private set; }

    private CaseScoring()
    {
        // Reserved for EF Core materialization.
    }

    public CaseScoring(
        Guid id, string reviewerId, Guid stepId, Guid competencyId, int score,
        DateTimeOffset scoredAt, decimal weight = 1m, string? notes = null, bool isAiGenerated = false)
        : base(id)
    {
        if (score is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(score), score, "A score must be between 0 and 100.");
        if (weight <= 0)
            throw new ArgumentOutOfRangeException(nameof(weight), weight, "A scoring's weight must be positive.");

        ReviewerId = reviewerId;
        StepId = stepId;
        CompetencyId = competencyId;
        Score = score;
        Weight = weight;
        Notes = notes;
        IsAiGenerated = isAiGenerated;
        ScoredAt = scoredAt;
    }
}
