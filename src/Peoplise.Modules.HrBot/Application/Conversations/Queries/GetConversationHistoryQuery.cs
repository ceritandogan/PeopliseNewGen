using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.Conversations.Queries;

public sealed record GetConversationHistoryQuery(Guid ConversationId) : IRequest<Result<ConversationHistory>>;

public sealed record ConversationHistory(
    Guid ConversationId,
    ConversationStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<ConversationLogEntry> Logs,
    IReadOnlyList<ConversationVariableEntry> Variables);

public sealed record ConversationLogEntry(Guid StepId, string? CandidateResponse, DateTimeOffset LoggedAt);

public sealed record ConversationVariableEntry(string Key, string Value);

/// <summary>Full transcript + captured variables for one conversation — what an HR reviewer sees.</summary>
public sealed class GetConversationHistoryQueryHandler : IRequestHandler<GetConversationHistoryQuery, Result<ConversationHistory>>
{
    private readonly AppDbContext _context;

    public GetConversationHistoryQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ConversationHistory>> Handle(GetConversationHistoryQuery request, CancellationToken cancellationToken)
    {
        var id = ConversationId.From(request.ConversationId);

        var conversation = await _context.Set<Conversation>()
            .Include(c => c.Logs)
            .Include(c => c.Variables)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (conversation is null)
            return Result.Failure<ConversationHistory>(Error.NotFound("Conversation.NotFound", $"No conversation '{request.ConversationId}' was found."));

        var history = new ConversationHistory(
            conversation.Id.Value,
            conversation.Status,
            conversation.StartedAt,
            conversation.CompletedAt,
            conversation.Logs.OrderBy(l => l.LoggedAt).Select(l => new ConversationLogEntry(l.StepId, l.CandidateResponse, l.LoggedAt)).ToList(),
            conversation.Variables.Select(v => new ConversationVariableEntry(v.Key, v.Value)).ToList());

        return Result.Success(history);
    }
}
