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
        // A position can have more than one BotProject (no uniqueness guard — see the
        // CaseBotProject/BotProject creation stage's design notes). Which project should
        // route a *new* candidate (most-recently-created, see StartConversationCommand)
        // and where an *existing* candidate's conversation actually lives are different
        // questions — a candidate who started chatting under an older project must stay
        // findable even after a newer project is created for the same position, so this
        // searches every BotProject for the position rather than only the newest.
        var botProjectIds = await _context.Set<BotProject>()
            .Where(p => p.PositionId == request.PositionId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var conversation = await _context.Set<Conversation>()
            .Where(c => c.CandidateId == request.CandidateId && botProjectIds.Contains(c.BotProjectId))
            .OrderByDescending(c => c.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return Result.Success<Guid?>(conversation?.Id.Value);
    }
}
