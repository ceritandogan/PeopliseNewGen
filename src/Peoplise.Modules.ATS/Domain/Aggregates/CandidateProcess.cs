using Peoplise.Modules.ATS.Domain.Entities;
using Peoplise.Modules.ATS.Domain.Events;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Auditing;
using Peoplise.SharedKernel.Domain;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Domain.Aggregates;

/// <summary>
/// One candidate's application to one position: pipeline state, notes, evaluations, and
/// the stage-transition rules that govern moving through a <see cref="WorkflowDefinition"/>.
/// A candidate applying to a different position gets a different
/// <see cref="CandidateProcess"/> — this is the per-application aggregate, not a
/// candidate's global profile.
/// </summary>
public sealed class CandidateProcess : AggregateRoot<CandidateProcessId>, IHasTenant, IAuditableEntity
{
    private static readonly PipelineStatus[] TerminalStatuses =
        [PipelineStatus.Accepted, PipelineStatus.Rejected, PipelineStatus.Eliminated, PipelineStatus.TimedOut];

    private readonly List<CandidateNote> _notes = [];
    private readonly List<CandidateEvaluation> _evaluations = [];
    private readonly List<CompletedStage> _completedStages = [];

    public CandidateId CandidateId { get; private set; } = null!;
    public PositionId PositionId { get; private set; } = null!;
    public WorkflowDefinitionId WorkflowDefinitionId { get; private set; } = null!;
    public PipelineStatus Status { get; private set; }
    public Guid? CurrentStageId { get; private set; }

    /// <summary>
    /// When the candidate entered <see cref="CurrentStageId"/> — what "days since the
    /// previous stage completed" (the delay-based workflow rule) is measured from.
    /// </summary>
    public DateTimeOffset CurrentStageEnteredAt { get; private set; }

    public string CandidateName { get; private set; } = string.Empty;
    public string CandidateEmail { get; private set; } = string.Empty;
    public string? CandidatePhone { get; private set; }
    public string? ResumeUrl { get; private set; }

    public TalentPoolEntry? TalentPoolEntry { get; private set; }

    public IReadOnlyCollection<CandidateNote> Notes => _notes.AsReadOnly();
    public IReadOnlyCollection<CandidateEvaluation> Evaluations => _evaluations.AsReadOnly();
    public IReadOnlyCollection<CompletedStage> CompletedStages => _completedStages.AsReadOnly();

    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    private CandidateProcess()
    {
        // Reserved for EF Core materialization.
    }

    private CandidateProcess(
        CandidateProcessId id,
        CandidateId candidateId,
        PositionId positionId,
        WorkflowDefinitionId workflowDefinitionId,
        string candidateName,
        string candidateEmail,
        string? candidatePhone,
        string? resumeUrl,
        Guid? firstStageId,
        DateTimeOffset appliedAt)
        : base(id)
    {
        CandidateId = candidateId;
        PositionId = positionId;
        WorkflowDefinitionId = workflowDefinitionId;
        CandidateName = candidateName;
        CandidateEmail = candidateEmail;
        CandidatePhone = candidatePhone;
        ResumeUrl = resumeUrl;
        CurrentStageId = firstStageId;
        CurrentStageEnteredAt = appliedAt;
        Status = PipelineStatus.NewApplication;
    }

    /// <summary>
    /// Starts a new application. Whether this candidate has *already* applied to this
    /// position ("Bir aday aynı pozisyona iki kez başvuramaz") is checked by the command
    /// handler that calls this factory, not here — that invariant spans every existing
    /// <see cref="CandidateProcess"/> for the pair, which a single aggregate instance has
    /// no way to see; see <c>SubmitCandidateApplicationCommandHandler</c>.
    /// </summary>
    public static CandidateProcess Submit(
        CandidateId candidateId,
        PositionId positionId,
        WorkflowDefinitionId workflowDefinitionId,
        string candidateName,
        string candidateEmail,
        string? candidatePhone,
        string? resumeUrl,
        Guid? firstStageId,
        DateTimeOffset appliedAt)
    {
        if (string.IsNullOrWhiteSpace(candidateName))
            throw new ArgumentException("A candidate must have a name.", nameof(candidateName));
        if (string.IsNullOrWhiteSpace(candidateEmail))
            throw new ArgumentException("A candidate must have an email.", nameof(candidateEmail));

        var process = new CandidateProcess(
            CandidateProcessId.New(), candidateId, positionId, workflowDefinitionId,
            candidateName, candidateEmail, candidatePhone, resumeUrl, firstStageId, appliedAt);

        process.Raise(new CandidateAppliedEvent(process.Id, candidateId, positionId));
        return process;
    }

    public bool IsInTerminalState => TerminalStatuses.Contains(Status);

    /// <summary>When <paramref name="stageId"/> was completed, or <c>null</c> if it hasn't been yet.</summary>
    public DateTimeOffset? GetCompletedAt(Guid stageId) =>
        _completedStages.FirstOrDefault(cs => cs.StageId == stageId)?.CompletedAt;

    /// <summary>Marks <see cref="CurrentStageId"/> as completed, without advancing past it.</summary>
    public Result CompleteCurrentStage(DateTimeOffset completedAt)
    {
        if (CurrentStageId is null)
            return Result.Failure(Error.Conflict("CandidateProcess.NoCurrentStage", "This process has no current stage to complete."));

        if (_completedStages.All(cs => cs.StageId != CurrentStageId.Value))
            _completedStages.Add(new CompletedStage(Guid.NewGuid(), CurrentStageId.Value, completedAt));

        Raise(new StageCompletedEvent(Id, CurrentStageId.Value));
        return Result.Success();
    }

    /// <summary>
    /// Moves to <paramref name="nextStageId"/>. Enforces "Aşama geçişi ancak önceki
    /// aşama tamamlanmışsa yapılabilir" — the *current* stage must already be marked
    /// completed via <see cref="CompleteCurrentStage"/> first.
    /// </summary>
    public Result MoveToNextStage(Guid nextStageId, DateTimeOffset movedAt)
    {
        if (IsInTerminalState)
            return Result.Failure(Error.Conflict("CandidateProcess.AlreadyClosed", "This process has already reached a terminal state."));

        if (CurrentStageId is null)
            return Result.Failure(Error.Conflict("CandidateProcess.NoCurrentStage", "This process has no current stage."));

        if (_completedStages.All(cs => cs.StageId != CurrentStageId.Value))
        {
            return Result.Failure(Error.Conflict(
                "CandidateProcess.PreviousStageNotCompleted",
                "Cannot move to the next stage before the current stage is completed."));
        }

        var fromStageId = CurrentStageId.Value;
        CurrentStageId = nextStageId;
        CurrentStageEnteredAt = movedAt;

        Raise(new CandidateMovedToNextStageEvent(Id, fromStageId, nextStageId));
        return Result.Success();
    }

    /// <summary>
    /// Records one evaluator's score for the current stage. An evaluator may only
    /// evaluate a given stage once — a correction is an explicit future feature, not a
    /// silent overwrite here.
    /// </summary>
    public Result SubmitEvaluation(string evaluatorId, EvaluationScore score, string? comments, DateTimeOffset submittedAt)
    {
        if (CurrentStageId is null)
            return Result.Failure(Error.Conflict("CandidateProcess.NoCurrentStage", "This process has no current stage to evaluate."));

        if (_evaluations.Any(e => e.StageId == CurrentStageId.Value && e.EvaluatorId == evaluatorId))
        {
            return Result.Failure(Error.Conflict(
                "CandidateProcess.DuplicateEvaluation",
                $"'{evaluatorId}' has already evaluated the current stage."));
        }

        _evaluations.Add(new CandidateEvaluation(Guid.NewGuid(), CurrentStageId.Value, evaluatorId, score, comments, submittedAt));
        Raise(new EvaluationSubmittedEvent(Id, evaluatorId, score));
        return Result.Success();
    }

    /// <summary>
    /// "Puan ortalaması hesaplanmadan konsensüs oluşturulamaz" — the consensus *is* the
    /// average; it cannot be produced (or overridden) without at least one evaluation to
    /// average over.
    /// </summary>
    public Result<EvaluationScore> CalculateConsensusScore(Guid stageId)
    {
        var stageScores = _evaluations.Where(e => e.StageId == stageId).Select(e => e.Score).ToList();

        if (stageScores.Count == 0)
        {
            return Result.Failure<EvaluationScore>(Error.Conflict(
                "CandidateProcess.NoEvaluationsYet",
                "Cannot form a consensus before at least one evaluation has been submitted for this stage."));
        }

        return Result.Success(EvaluationScore.Average(stageScores));
    }

    public Result Eliminate(string reason)
    {
        if (IsInTerminalState)
            return Result.Failure(Error.Conflict("CandidateProcess.AlreadyClosed", "This process has already reached a terminal state."));

        Status = PipelineStatus.Eliminated;
        Raise(new CandidateEliminatedEvent(Id, reason));
        return Result.Success();
    }

    public Result MarkTimedOut()
    {
        if (IsInTerminalState)
            return Result.Failure(Error.Conflict("CandidateProcess.AlreadyClosed", "This process has already reached a terminal state."));

        Status = PipelineStatus.TimedOut;
        return Result.Success();
    }

    public Result Accept()
    {
        if (IsInTerminalState)
            return Result.Failure(Error.Conflict("CandidateProcess.AlreadyClosed", "This process has already reached a terminal state."));

        Status = PipelineStatus.Accepted;
        return Result.Success();
    }

    public void AddNote(string authorId, string text, bool isPrivate, DateTimeOffset createdAt) =>
        _notes.Add(new CandidateNote(Guid.NewGuid(), authorId, text, isPrivate, createdAt));

    public void AddToTalentPool(IEnumerable<string> tags, string? note, DateTimeOffset addedAt) =>
        TalentPoolEntry = new TalentPoolEntry(Guid.NewGuid(), tags, note, addedAt);
}
