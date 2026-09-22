using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.Conversations.Queries;

/// <summary>
/// Finds the conversation (if any) a candidate has for one specific position — the panel
/// has a `CandidateProcess` (ATS) and needs to locate its matching HrBot `Conversation`,
/// which are linked only by sharing the same raw `CandidateId` and both being scoped to
/// the same position (via `BotProject.PositionId`), never a direct foreign key across
/// modules. No conversation existing yet is a normal, successful outcome (the candidate
/// hasn't started chatting), not a failure — callers get `null`, not `NotFound`.
/// </summary>
public sealed record GetConversationForCandidateQuery(Guid CandidateId, Guid PositionId) : IRequest<Result<Guid?>>;

public sealed class GetConversationForCandidateQueryHandler : IRequestHandler<GetConversationForCandidateQuery, Result<Guid?>>
{
    private readonly AppDbContext _context;

    public GetConversationForCandidateQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid?>> Handle(GetConversationForCandidateQuery request, CancellationToken cancellationToken)
    {
        var botProject = await _context.Set<BotProject>()
            .SingleOrDefaultAsync(p => p.PositionId == request.PositionId, cancellationToken);

        if (botProject is null)
            return Result.Success<Guid?>(null);

        var conversation = await _context.Set<Conversation>()
            .Where(c => c.CandidateId == request.CandidateId && c.BotProjectId == botProject.Id)
            .OrderByDescending(c => c.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return Result.Success<Guid?>(conversation?.Id.Value);
    }
}
