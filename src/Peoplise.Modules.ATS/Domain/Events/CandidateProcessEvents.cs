using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.ATS.Domain.Events;

public sealed record CandidateAppliedEvent(CandidateProcessId ProcessId, CandidateId CandidateId, PositionId PositionId)
    : IDomainEvent;

public sealed record CandidateMovedToNextStageEvent(CandidateProcessId ProcessId, Guid FromStageId, Guid ToStageId)
    : IDomainEvent;

public sealed record CandidateEliminatedEvent(CandidateProcessId ProcessId, string Reason) : IDomainEvent;

public sealed record StageCompletedEvent(CandidateProcessId ProcessId, Guid StageId) : IDomainEvent;

public sealed record EvaluationSubmittedEvent(CandidateProcessId ProcessId, string EvaluatorId, EvaluationScore Score)
    : IDomainEvent;
