using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Candidates.Queries;

/// <summary>
/// Backs the manual-add flow's own duplicate check (<c>CandidatesController.AddManual</c>).
/// <see cref="SubmitCandidateApplicationCommand"/>'s own duplicate guard is keyed by
/// <c>CandidateId</c> — the right check for the public apply form, where the same real
/// applicant's browser resubmits with the same client-generated id. Manual add mints a
/// fresh server-side <c>CandidateId</c> on every call (see AddManual's remarks), so that
/// check alone would never catch HR adding the same email to the same position twice —
/// this query checks by email instead, scoped to the position, same as
/// <see cref="GetCandidateContactQuery"/>.
/// </summary>
public sealed record CandidateExistsForPositionQuery(Guid PositionId, string CandidateEmail) : IRequest<Result<bool>>;

public sealed class CandidateExistsForPositionQueryHandler : IRequestHandler<CandidateExistsForPositionQuery, Result<bool>>
{
    private readonly AppDbContext _context;

    public CandidateExistsForPositionQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(CandidateExistsForPositionQuery request, CancellationToken cancellationToken)
    {
        var positionId = PositionId.From(request.PositionId);

        var exists = await _context.Set<CandidateProcess>()
            .AnyAsync(p => p.PositionId == positionId && p.CandidateEmail == request.CandidateEmail, cancellationToken);

        return Result.Success(exists);
    }
}
