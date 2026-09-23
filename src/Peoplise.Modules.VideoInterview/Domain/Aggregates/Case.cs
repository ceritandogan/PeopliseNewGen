using Peoplise.Modules.VideoInterview.Domain.Entities;
using Peoplise.Modules.VideoInterview.Domain.Events;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.AI;
using Peoplise.SharedKernel.Auditing;
using Peoplise.SharedKernel.Domain;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Domain.Aggregates;

/// <summary>One candidate's session through a <c>CaseBotProject</c>: their responses, every reviewer's scoring, and the computed result.</summary>
public sealed class Case : AggregateRoot<CaseId>, IHasTenant, IAuditableEntity
{
    private static readonly CaseStatus[] TerminalStatuses =
        [CaseStatus.Completed, CaseStatus.TimedOut, CaseStatus.ConsentWithdrawn, CaseStatus.RetentionExpired];

    private readonly List<CaseStepConversation> _stepConversations = [];
    private readonly List<StepRetakeCount> _retakeCounts = [];
    private readonly List<CaseScoring> _scorings = [];
    private readonly List<CaseCodeReview> _codeReviews = [];
    private readonly List<Report> _reports = [];

    public CaseBotProjectId CaseBotProjectId { get; private set; } = null!;

    /// <summary>Raw <see cref="Guid"/> — a candidate reference into the ATS module, not that module's own <c>CandidateId</c> type. Cleared to <see cref="Guid.Empty"/> on consent withdrawal / retention expiry.</summary>
    public Guid CandidateId { get; private set; }

    public CaseStatus Status { get; private set; }
    public Guid CurrentFlowId { get; private set; }
    public Guid? CurrentStepId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public CaseResult? AssessmentResult { get; private set; }

    public IReadOnlyCollection<CaseStepConversation> StepConversations => _stepConversations.AsReadOnly();
    public IReadOnlyCollection<StepRetakeCount> RetakeCounts => _retakeCounts.AsReadOnly();
    public IReadOnlyCollection<CaseScoring> Scorings => _scorings.AsReadOnly();
    public IReadOnlyCollection<CaseCodeReview> CodeReviews => _codeReviews.AsReadOnly();
    public IReadOnlyCollection<Report> Reports => _reports.AsReadOnly();

    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    private Case()
    {
        // Reserved for EF Core materialization.
    }

    private Case(CaseId id, CaseBotProjectId caseBotProjectId, Guid candidateId, Guid initialFlowId, Guid? initialStepId, DateTimeOffset startedAt)
        : base(id)
    {
        CaseBotProjectId = caseBotProjectId;
        CandidateId = candidateId;
        CurrentFlowId = initialFlowId;
        CurrentStepId = initialStepId;
        Status = CaseStatus.InProgress;
        StartedAt = startedAt;
    }

    public static Case Start(CaseBotProjectId caseBotProjectId, Guid candidateId, Guid initialFlowId, Guid? initialStepId, DateTimeOffset startedAt)
    {
        var @case = new Case(CaseId.New(), caseBotProjectId, candidateId, initialFlowId, initialStepId, startedAt);
        @case.Raise(new CaseStartedEvent(@case.Id, caseBotProjectId, candidateId));
        return @case;
    }

    public bool IsOpen => !TerminalStatuses.Contains(Status);

    public Result RecordTextResponse(Guid stepId, string text, DateTimeOffset respondedAt)
    {
        if (!IsOpen)
            return Result.Failure(Error.Conflict("Case.AlreadyClosed", "This case has already ended."));

        _stepConversations.Add(new CaseStepConversation(Guid.NewGuid(), stepId, respondedAt, textResponse: text));
        return Result.Success();
    }

    public Result RecordDocumentUpload(Guid stepId, string documentUrl, DateTimeOffset respondedAt)
    {
        if (!IsOpen)
            return Result.Failure(Error.Conflict("Case.AlreadyClosed", "This case has already ended."));

        _stepConversations.Add(new CaseStepConversation(Guid.NewGuid(), stepId, respondedAt, documentUrl: documentUrl));
        return Result.Success();
    }

    /// <summary>
    /// Stores a video answer, enforcing "Tekrar Çekim Hakkı": the first recording at a
    /// step is free; every subsequent one at the same step is a retake and is rejected
    /// once <paramref name="retakesAllowed"/> is used up. The policy itself lives on
    /// <c>CaseBotProject</c>, not here — passed in rather than reached for across
    /// aggregates.
    /// </summary>
    public Result RecordVideoAnswer(Guid stepId, string videoUrl, int retakesAllowed, DateTimeOffset respondedAt)
    {
        if (!IsOpen)
            return Result.Failure(Error.Conflict("Case.AlreadyClosed", "This case has already ended."));

        var existing = _stepConversations.FirstOrDefault(c => c.StepId == stepId && c.VideoUrl is not null);
        if (existing is not null)
        {
            var retakeCount = _retakeCounts.FirstOrDefault(r => r.StepId == stepId);
            if (retakeCount is null)
            {
                retakeCount = new StepRetakeCount(Guid.NewGuid(), stepId);
                _retakeCounts.Add(retakeCount);
            }

            if (retakeCount.RetakesUsed >= retakesAllowed)
            {
                return Result.Failure(Error.Conflict(
                    "Case.RetakeLimitExceeded", $"No retakes remaining for this step (limit: {retakesAllowed})."));
            }

            retakeCount.Increment();
            _stepConversations.Remove(existing);
        }

        var conversation = new CaseStepConversation(Guid.NewGuid(), stepId, respondedAt, videoUrl: videoUrl);
        _stepConversations.Add(conversation);

        Raise(new VideoRecordedEvent(Id, conversation.Id, videoUrl));
        return Result.Success();
    }

    /// <summary>Called by the background transcription job once <see cref="IAIProvider.TranscribeAsync"/> completes.</summary>
    public Result CompleteTranscription(Guid stepConversationId, string transcriptText)
    {
        var conversation = _stepConversations.FirstOrDefault(c => c.Id == stepConversationId);
        if (conversation is null)
            return Result.Failure(Error.NotFound("Case.StepConversationNotFound", "No such step conversation on this case."));

        conversation.SetTranscript(transcriptText);
        Raise(new TranscriptionCompletedEvent(Id, stepConversationId));
        return Result.Success();
    }

    public Result SubmitScoring(
        string reviewerId, Guid stepId, Guid competencyId, int score, DateTimeOffset scoredAt,
        decimal weight = 1m, string? notes = null, bool isAiGenerated = false)
    {
        // Unlike RecordTextResponse/RecordVideoAnswer, scoring is expected to happen on
        // an InProgress case (as steps complete) or a Completed one (a reviewer scoring
        // after the fact) — only the two anonymized-data states are rejected, since
        // adding new scoring against a case whose identity/media has already been
        // severed (Stage 10/12's KVKK sweep) would defeat the point of anonymizing it.
        if (Status is CaseStatus.ConsentWithdrawn or CaseStatus.RetentionExpired)
            return Result.Failure(Error.Conflict("Case.DataAnonymized", "This case's data has already been anonymized; it can no longer be scored."));

        if (_scorings.Any(s => s.ReviewerId == reviewerId && s.StepId == stepId && s.CompetencyId == competencyId))
        {
            return Result.Failure(Error.Conflict(
                "Case.DuplicateScoring", $"'{reviewerId}' has already scored this step against this competency."));
        }

        _scorings.Add(new CaseScoring(Guid.NewGuid(), reviewerId, stepId, competencyId, score, scoredAt, weight, notes, isAiGenerated));
        Raise(new ScoringSubmittedEvent(Id, reviewerId, stepId));
        return Result.Success();
    }

    public Result RecordCodeReview(Guid stepId, CodeReviewResult review, DateTimeOffset reviewedAt)
    {
        _codeReviews.Add(new CaseCodeReview(
            Guid.NewGuid(), stepId, review.Readability, review.Functionality,
            review.DataValidation, review.UseCaseHandling, review.Syntax, reviewedAt));
        return Result.Success();
    }

    /// <summary>
    /// "Yetkinlik puanlama hesaplaması (kısmi puan, ağırlıklı ortalama)" — every
    /// scoring for this competency, across every step and reviewer, combined into one
    /// weighted average: Σ(score×weight) / Σ(weight).
    /// </summary>
    public Result<decimal> CalculateCompetencyResult(Guid competencyId)
    {
        var relevant = _scorings.Where(s => s.CompetencyId == competencyId).ToList();
        if (relevant.Count == 0)
            return Result.Failure<decimal>(Error.Conflict("Case.NoScoringsYet", "No scorings exist yet for this competency."));

        var weightedSum = relevant.Sum(s => s.Score * s.Weight);
        var totalWeight = relevant.Sum(s => s.Weight);

        return Result.Success(Math.Round(weightedSum / totalWeight, 2));
    }

    /// <summary>
    /// Every competency this case has at least one scoring for, right now — computed
    /// fresh from <see cref="Scorings"/> on every call, never cached. This is the source
    /// of truth for reads (comparison, reports): <see cref="AssessmentResult"/> is only
    /// ever a one-time snapshot taken when <see cref="Complete"/> ran, and a reviewer
    /// scoring a case after it already completed — the normal order, since scoring is
    /// post-hoc human review — would never be reflected there.
    /// </summary>
    public IReadOnlyList<Entities.CompetencyResult> GetCurrentCompetencyResults()
    {
        var results = new List<Entities.CompetencyResult>();
        foreach (var competencyId in _scorings.Select(s => s.CompetencyId).Distinct())
        {
            var competencyScore = CalculateCompetencyResult(competencyId);
            if (competencyScore.IsSuccess)
                results.Add(new Entities.CompetencyResult(Guid.NewGuid(), competencyId, competencyScore.Value));
        }

        return results;
    }

    public Result Complete(DateTimeOffset completedAt)
    {
        if (!IsOpen)
            return Result.Failure(Error.Conflict("Case.AlreadyClosed", "This case has already ended."));

        // A write-only historical snapshot from this exact moment — kept for the KVKK
        // "statistical data preserved" intent behind AnonymizeMediaAndIdentity (see its
        // remarks), not read back by anything. Reads always go through
        // GetCurrentCompetencyResults() instead — see ADR 0006.
        var result = new CaseResult(Guid.NewGuid(), completedAt);
        foreach (var competencyResult in GetCurrentCompetencyResults())
            result.AddCompetencyResult(competencyResult);

        AssessmentResult = result;
        Status = CaseStatus.Completed;
        CompletedAt = completedAt;
        Raise(new CaseCompletedEvent(Id));
        return Result.Success();
    }

    public Result WithdrawConsent(string reason, DateTimeOffset now)
    {
        if (!IsOpen)
            return Result.Failure(Error.Conflict("Case.AlreadyClosed", "This case has already ended."));

        AnonymizeMediaAndIdentity();
        Status = CaseStatus.ConsentWithdrawn;
        CompletedAt = now;
        Raise(new ConsentWithdrawnEvent(Id, reason));
        return Result.Success();
    }

    /// <summary>
    /// Unlike <see cref="WithdrawConsent"/>, this is expected to run on an already
    /// <see cref="CaseStatus.Completed"/>/<see cref="CaseStatus.TimedOut"/> case — that's
    /// the common case a retention sweep targets, data that simply aged out after the
    /// interview finished — so it only rejects a case whose data is already anonymized,
    /// not every non-open one.
    /// </summary>
    public Result ExpireRetention(DateTimeOffset now)
    {
        if (Status is CaseStatus.ConsentWithdrawn or CaseStatus.RetentionExpired)
            return Result.Failure(Error.Conflict("Case.AlreadyAnonymized", "This case's data has already been anonymized."));

        AnonymizeMediaAndIdentity();
        Status = CaseStatus.RetentionExpired;
        // Preserve the true completion date for a case that already finished — only a
        // still-open (abandoned, never-completed) case gets CompletedAt set here.
        CompletedAt ??= now;
        Raise(new DataRetentionExpiredEvent(Id));
        return Result.Success();
    }

    /// <summary>
    /// "video/doküman dosyaları silinir; puanlama ve rapor kayıtları anonimleştirilir
    /// (aday kimliği koparılır, istatistiksel veri korunur)" — media references are
    /// cleared (the caller deletes the actual files via <c>IFileStorageService</c>
    /// first) and the candidate identity is severed, but scorings/results/reports stay,
    /// intact, for aggregate statistics.
    /// </summary>
    private void AnonymizeMediaAndIdentity()
    {
        foreach (var conversation in _stepConversations)
            conversation.ClearMediaReferences();

        CandidateId = Guid.Empty;
    }

    public Report GenerateReport(ReportTemplate template, DateTimeOffset generatedAt)
    {
        var report = new Report(Guid.NewGuid(), template.Id, generatedAt);

        foreach (var section in template.Sections)
        {
            var content = BuildSectionContent(section.Title);
            report.AddSection(new ReportSectionContent(Guid.NewGuid(), section.Id, section.Title, content));
        }

        _reports.Add(report);
        return report;
    }

    private string BuildSectionContent(string sectionTitle)
    {
        var isCompetencySection = sectionTitle.Contains("competenc", StringComparison.OrdinalIgnoreCase)
            || sectionTitle.Contains("yetkinlik", StringComparison.OrdinalIgnoreCase);

        if (isCompetencySection)
        {
            var currentResults = GetCurrentCompetencyResults();
            if (currentResults.Count > 0)
            {
                var lines = currentResults.Select(r => $"{r.CompetencyId}: {r.Score:0.##}/100");
                return string.Join(Environment.NewLine, lines);
            }
        }

        if (isCompetencySection)
            return "No competency scores available yet.";

        return string.Empty;
    }
}
