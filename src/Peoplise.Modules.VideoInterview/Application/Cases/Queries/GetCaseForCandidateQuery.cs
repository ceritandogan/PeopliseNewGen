using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.Cases.Queries;

/// <summary>
/// Finds the case (if any) a candidate has for one specific position — the panel has a
/// `CandidateProcess` (ATS) and needs to locate its matching VideoInterview `Case`, which
/// are linked only by sharing the same raw `CandidateId` and both being scoped to the
/// same position, never a direct foreign key across modules. No case existing yet is a
/// normal, successful outcome (the candidate hasn't started their video interview), not a
/// failure — callers get `null`, not `NotFound`. Mirrors
/// <c>GetConversationForCandidateQuery</c>: searches every `CaseBotProject` for the
/// position, not just the most-recently-created one, so a candidate who started under an
/// older project stays findable after a newer project is created for the same position.
/// </summary>
public sealed record GetCaseForCandidateQuery(Guid CandidateId, Guid PositionId) : IRequest<Result<Guid?>>;

public sealed class GetCaseForCandidateQueryHandler : IRequestHandler<GetCaseForCandidateQuery, Result<Guid?>>
{
    private readonly AppDbContext _context;

    public GetCaseForCandidateQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid?>> Handle(GetCaseForCandidateQuery request, CancellationToken cancellationToken)
    {
        var caseBotProjectIds = await _context.Set<CaseBotProject>()
            .Where(p => p.PositionId == request.PositionId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var @case = await _context.Set<Case>()
            .Where(c => c.CandidateId == request.CandidateId && caseBotProjectIds.Contains(c.CaseBotProjectId))
            .OrderByDescending(c => c.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return Result.Success<Guid?>(@case?.Id.Value);
    }
}
