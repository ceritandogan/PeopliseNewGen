using Peoplise.Modules.HrBot.Domain.Entities;
using Peoplise.Modules.HrBot.Domain.Events;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Auditing;
using Peoplise.SharedKernel.Domain;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Domain.Aggregates;

/// <summary>One candidate's run through a <c>BotProject</c>'s flow(s).</summary>
public sealed class Conversation : AggregateRoot<ConversationId>, IHasTenant, IAuditableEntity
{
    private readonly List<ConversationVariable> _variables = [];
    private readonly List<ConversationLog> _logs = [];

    public BotProjectId BotProjectId { get; private set; } = null!;

    /// <summary>Raw <see cref="Guid"/> — a candidate reference into the ATS module, not that module's own <c>CandidateId</c> type.</summary>
    public Guid CandidateId { get; private set; }

    public ConversationInterface Interface { get; private set; }
    public ConversationStatus Status { get; private set; }
    public Guid CurrentFlowId { get; private set; }
    public Guid? CurrentStepId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyCollection<ConversationVariable> Variables => _variables.AsReadOnly();
    public IReadOnlyCollection<ConversationLog> Logs => _logs.AsReadOnly();

    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    private Conversation()
    {
        // Reserved for EF Core materialization.
    }

    private Conversation(
        ConversationId id, BotProjectId botProjectId, Guid candidateId,
        ConversationInterface @interface, Guid initialFlowId, Guid? initialStepId, DateTimeOffset startedAt)
        : base(id)
    {
        BotProjectId = botProjectId;
        CandidateId = candidateId;
        Interface = @interface;
        CurrentFlowId = initialFlowId;
        CurrentStepId = initialStepId;
        Status = ConversationStatus.InProgress;
        StartedAt = startedAt;
    }

    public static Conversation Start(
        BotProjectId botProjectId, Guid candidateId, ConversationInterface @interface,
        Guid initialFlowId, Guid? initialStepId, DateTimeOffset startedAt)
    {
        var conversation = new Conversation(
            ConversationId.New(), botProjectId, candidateId, @interface, initialFlowId, initialStepId, startedAt);

        conversation.Raise(new ConversationStartedEvent(conversation.Id, botProjectId, candidateId));
        return conversation;
    }

    public bool IsOpen => Status == ConversationStatus.InProgress;

    public Result RecordStep(Guid stepId, string? candidateResponse, DateTimeOffset loggedAt)
    {
        if (!IsOpen)
            return Result.Failure(Error.Conflict("Conversation.AlreadyClosed", "This conversation has already ended."));

        _logs.Add(new ConversationLog(Guid.NewGuid(), stepId, candidateResponse, loggedAt));
        return Result.Success();
    }

    public void SetVariable(string key, string value, DateTimeOffset capturedAt)
    {
        var existing = _variables.FirstOrDefault(v => v.Key == key);
        if (existing is not null)
            _variables.Remove(existing);

        _variables.Add(new ConversationVariable(Guid.NewGuid(), key, value, capturedAt));
    }

    public Result WithdrawConsent(string reason, DateTimeOffset now)
    {
        if (!IsOpen)
            return Result.Failure(Error.Conflict("Conversation.AlreadyClosed", "This conversation has already ended."));

        AnonymizeIdentityAndContent();
        Status = ConversationStatus.ConsentWithdrawn;
        CompletedAt = now;
        Raise(new ConsentWithdrawnEvent(Id, reason));
        return Result.Success();
    }

    /// <summary>
    /// Unlike <see cref="WithdrawConsent"/>, this is expected to run on an already
    /// <see cref="ConversationStatus.Completed"/>/<see cref="ConversationStatus.ScreenedOut"/>/
    /// <see cref="ConversationStatus.TimedOut"/> conversation — that's the common case a
    /// retention sweep targets — so it only rejects a conversation whose data is already
    /// anonymized, not every non-open one. Mirrors the fix applied to VideoInterview's
    /// <c>Case.ExpireRetention</c> (see docs/adr/0001).
    /// </summary>
    public Result ExpireRetention(DateTimeOffset now)
    {
        if (Status is ConversationStatus.ConsentWithdrawn or ConversationStatus.RetentionExpired)
            return Result.Failure(Error.Conflict("Conversation.AlreadyAnonymized", "This conversation's data has already been anonymized."));

        AnonymizeIdentityAndContent();
        Status = ConversationStatus.RetentionExpired;
        // Preserve the true completion date for a conversation that already finished —
        // only a still-open (abandoned, never-completed) conversation gets CompletedAt
        // set here.
        CompletedAt ??= now;
        Raise(new DataRetentionExpiredEvent(Id));
        return Result.Success();
    }

    /// <summary>
    /// Unlike VideoInterview's Case (which keeps Scorings/Reports as non-identifying
    /// aggregate statistics), there's nothing here worth preserving separately from the
    /// candidate's identity: a captured variable (salary expectation, location) or a
    /// logged response IS the personal data, not a derived score. So every log's
    /// response and every variable's value is cleared, alongside the identity link —
    /// StepId/LoggedAt/Key/CapturedAt survive, giving the transcript's shape without its
    /// content.
    /// </summary>
    private void AnonymizeIdentityAndContent()
    {
        foreach (var log in _logs)
            log.ClearResponse();

        foreach (var variable in _variables)
            variable.ClearValue();

        CandidateId = Guid.Empty;
    }

    public Result MoveToStep(Guid flowId, Guid stepId)
    {
        if (!IsOpen)
            return Result.Failure(Error.Conflict("Conversation.AlreadyClosed", "This conversation has already ended."));

        CurrentFlowId = flowId;
        CurrentStepId = stepId;
        return Result.Success();
    }

    public Result Complete(DateTimeOffset completedAt)
    {
        if (!IsOpen)
            return Result.Failure(Error.Conflict("Conversation.AlreadyClosed", "This conversation has already ended."));

        Status = ConversationStatus.Completed;
        CompletedAt = completedAt;
        Raise(new ConversationCompletedEvent(Id));
        return Result.Success();
    }

    public Result ScreenOut(string reason, DateTimeOffset completedAt)
    {
        if (!IsOpen)
            return Result.Failure(Error.Conflict("Conversation.AlreadyClosed", "This conversation has already ended."));

        Status = ConversationStatus.ScreenedOut;
        CompletedAt = completedAt;
        Raise(new CandidateScreenedOutEvent(Id, reason));
        return Result.Success();
    }

    public Result MarkTimedOut(DateTimeOffset timedOutAt)
    {
        if (!IsOpen)
            return Result.Failure(Error.Conflict("Conversation.AlreadyClosed", "This conversation has already ended."));

        Status = ConversationStatus.TimedOut;
        CompletedAt = timedOutAt;
        return Result.Success();
    }

    public void LogUnmatchedQuestion(string questionText) =>
        Raise(new UnmatchedQuestionLoggedEvent(Id, BotProjectId, questionText));
}
