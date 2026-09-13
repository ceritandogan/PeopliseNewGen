using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.ATS.Domain.Entities;

/// <summary>One evaluator's score and comments for a candidate at a specific stage.</summary>
public sealed class CandidateEvaluation : BaseEntity<Guid>
{
    public Guid StageId { get; private set; }
    public string EvaluatorId { get; private set; } = string.Empty;
    public EvaluationScore Score { get; private set; } = null!;
    public string? Comments { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }

    private CandidateEvaluation()
    {
        // Reserved for EF Core materialization.
    }

    public CandidateEvaluation(
        Guid id,
        Guid stageId,
        string evaluatorId,
        EvaluationScore score,
        string? comments,
        DateTimeOffset submittedAt)
        : base(id)
    {
        StageId = stageId;
        EvaluatorId = evaluatorId;
        Score = score;
        Comments = comments;
        SubmittedAt = submittedAt;
    }
}
