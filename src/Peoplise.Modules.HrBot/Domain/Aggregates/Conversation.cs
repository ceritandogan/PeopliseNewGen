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
