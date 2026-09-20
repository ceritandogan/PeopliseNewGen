using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.HrBot.Domain.Events;

public sealed record ConversationStartedEvent(ConversationId ConversationId, BotProjectId BotProjectId, Guid CandidateId)
    : IDomainEvent;

public sealed record ConversationCompletedEvent(ConversationId ConversationId) : IDomainEvent;

public sealed record CandidateScreenedOutEvent(ConversationId ConversationId, string Reason) : IDomainEvent;

public sealed record UnmatchedQuestionLoggedEvent(ConversationId ConversationId, BotProjectId BotProjectId, string QuestionText)
    : IDomainEvent;

public sealed record ConsentWithdrawnEvent(ConversationId ConversationId, string Reason) : IDomainEvent;

public sealed record DataRetentionExpiredEvent(ConversationId ConversationId) : IDomainEvent;
