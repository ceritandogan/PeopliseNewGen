using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Candidates.Queries;

/// <summary>
/// Resolves a raw candidate id (as VideoInterview's <c>Case.CandidateId</c> or HrBot's
/// <c>Conversation.CandidateId</c> carry it) to the email/name captured on application —
/// candidate-link delivery's only source of a candidate's email, since neither HrBot nor
/// VideoInterview know it themselves. Scoped to one position, same as
/// <see cref="GetCandidateNamesQuery"/>. A candidate id with no matching CandidateProcess
/// is a not-found failure, not an empty result — unlike the batch name lookup, a caller here
/// always wants exactly one contact to send to.
/// </summary>
public sealed record GetCandidateContactQuery(Guid PositionId, Guid CandidateId) : IRequest<Result<CandidateContactDto>>;

public sealed record CandidateContactDto(string Email, string Name);

public sealed class GetCandidateContactQueryHandler : IRequestHandler<GetCandidateContactQuery, Result<CandidateContactDto>>
{
    private readonly AppDbContext _context;

    public GetCandidateContactQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CandidateContactDto>> Handle(GetCandidateContactQuery request, CancellationToken cancellationToken)
    {
        var positionId = PositionId.From(request.PositionId);
        var candidateId = CandidateId.From(request.CandidateId);

        var contact = await _context.Set<CandidateProcess>()
            .Where(p => p.PositionId == positionId && p.CandidateId == candidateId)
            .Select(p => new { p.CandidateEmail, p.CandidateName })
            .SingleOrDefaultAsync(cancellationToken);

        if (contact is null)
        {
            return Result.Failure<CandidateContactDto>(Error.NotFound("Candidate.NotFound", "No candidate application found for this candidate/position."));
        }

        return Result.Success(new CandidateContactDto(contact.CandidateEmail, contact.CandidateName));
    }
}
