using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.VideoInterview.Domain.Events;

public sealed record CaseStartedEvent(CaseId CaseId, CaseBotProjectId CaseBotProjectId, Guid CandidateId) : IDomainEvent;

public sealed record CaseCompletedEvent(CaseId CaseId) : IDomainEvent;

/// <summary>Raised when a video answer is stored — the trigger the background transcription job listens for.</summary>
public sealed record VideoRecordedEvent(CaseId CaseId, Guid StepConversationId, string VideoUrl) : IDomainEvent;

public sealed record TranscriptionCompletedEvent(CaseId CaseId, Guid StepConversationId) : IDomainEvent;

public sealed record ScoringSubmittedEvent(CaseId CaseId, string ReviewerId, Guid StepId) : IDomainEvent;

public sealed record ConsentWithdrawnEvent(CaseId CaseId, string Reason) : IDomainEvent;

public sealed record DataRetentionExpiredEvent(CaseId CaseId) : IDomainEvent;
