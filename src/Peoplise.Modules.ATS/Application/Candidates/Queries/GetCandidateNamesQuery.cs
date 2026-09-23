using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Candidates.Queries;

/// <summary>
/// Resolves raw candidate ids (as VideoInterview's <c>Case.CandidateId</c> or HrBot's
/// <c>Conversation.CandidateId</c> carry them) to display names and the owning
/// <c>CandidateProcess</c>'s own id (for panel views that need to link through to
/// <c>CandidateDetailPage</c>, which routes on <c>CandidateProcessId</c>, not <c>CandidateId</c>),
/// for panel views like the competency comparison table that only ever see a bare candidate id.
/// Candidate id is minted fresh per apply-form submission (not a stable person id), so this is
/// scoped to one position — the same pair (candidateId, positionId) every other cross-module
/// candidate lookup uses. An id with no matching CandidateProcess is simply omitted, not a failure.
/// </summary>
public sealed record GetCandidateNamesQuery(Guid PositionId, IReadOnlyList<Guid> CandidateIds) : IRequest<Result<IReadOnlyDictionary<Guid, CandidateNameDto>>>;

public sealed record CandidateNameDto(Guid CandidateProcessId, string Name);

public sealed class GetCandidateNamesQueryHandler : IRequestHandler<GetCandidateNamesQuery, Result<IReadOnlyDictionary<Guid, CandidateNameDto>>>
{
    private readonly AppDbContext _context;

    public GetCandidateNamesQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyDictionary<Guid, CandidateNameDto>>> Handle(GetCandidateNamesQuery request, CancellationToken cancellationToken)
    {
        var positionId = PositionId.From(request.PositionId);
        var candidateIds = request.CandidateIds.Select(CandidateId.From).ToList();

        var names = await _context.Set<CandidateProcess>()
            .Where(p => p.PositionId == positionId && candidateIds.Contains(p.CandidateId))
            .Select(p => new { p.Id, p.CandidateId, p.CandidateName })
            .ToListAsync(cancellationToken);

        IReadOnlyDictionary<Guid, CandidateNameDto> result = names.ToDictionary(
            n => n.CandidateId.Value,
            n => new CandidateNameDto(n.Id.Value, n.CandidateName));
        return Result.Success(result);
    }
}
