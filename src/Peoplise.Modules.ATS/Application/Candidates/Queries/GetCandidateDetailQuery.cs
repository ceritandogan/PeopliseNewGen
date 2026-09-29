using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Identity;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Candidates.Queries;

public sealed record GetCandidateDetailQuery(Guid CandidateProcessId) : IRequest<Result<CandidateDetail>>;

public sealed record CandidateDetail(
    Guid CandidateProcessId,
    Guid CandidateId,
    Guid PositionId,
    string CandidateName,
    string CandidateEmail,
    string? CandidatePhone,
    string? ResumeUrl,
    PipelineStatus Status,
    Guid? CurrentStageId,
    IReadOnlyList<CandidateNoteDetail> Notes,
    IReadOnlyList<CandidateEvaluationDetail> Evaluations);

public sealed record CandidateNoteDetail(string AuthorId, string Text, bool IsPrivate, DateTimeOffset CreatedAt);

/// <summary>
/// <see cref="EvaluatorId"/> is the JWT <c>sub</c> claim — a stable id, kept exactly as
/// submitted, since the handler's own duplicate-evaluation guard is keyed by it and must
/// keep working even if a reviewer's email later changes. <see cref="EvaluatorEmail"/> is
/// resolved separately, purely for display — see GetCandidateDetailQueryHandler's remarks.
/// </summary>
public sealed record CandidateEvaluationDetail(Guid StageId, string EvaluatorId, string EvaluatorEmail, decimal Score, string? Comments, DateTimeOffset SubmittedAt);

/// <summary>Full profile + all evaluations for one candidate's application — a single-record detail view, not a list, so loading the whole aggregate (including its child collections) is the right cost/simplicity trade-off here.</summary>
public sealed class GetCandidateDetailQueryHandler : IRequestHandler<GetCandidateDetailQuery, Result<CandidateDetail>>
{
    private readonly AppDbContext _context;

    public GetCandidateDetailQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CandidateDetail>> Handle(GetCandidateDetailQuery request, CancellationToken cancellationToken)
    {
        var id = CandidateProcessId.From(request.CandidateProcessId);

        var process = await _context.Set<CandidateProcess>()
            .Include(p => p.Notes)
            .Include(p => p.Evaluations)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (process is null)
            return Result.Failure<CandidateDetail>(Error.NotFound("CandidateProcess.NotFound", $"No candidate process '{request.CandidateProcessId}' was found."));

        // EvaluatorId is the reviewer's stable OpenIddict user id (see
        // CandidatesController.SubmitEvaluation), not a human-readable label — resolved
        // here against Peoplise.Infrastructure.Identity.User (the same AppDbContext every
        // module already shares) purely for display, the same reasoning the CaseReport
        // competency-id fix already applied to a different raw-id-on-screen case.
        var evaluatorIds = process.Evaluations
            .Select(e => Guid.TryParse(e.EvaluatorId, out var parsed) ? parsed : (Guid?)null)
            .Where(parsed => parsed.HasValue)
            .Select(parsed => parsed!.Value)
            .Distinct()
            .ToList();

        var evaluatorEmailsById = await _context.Set<User>()
            .Where(u => evaluatorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        var detail = new CandidateDetail(
            process.Id.Value,
            process.CandidateId.Value,
            process.PositionId.Value,
            process.CandidateName,
            process.CandidateEmail,
            process.CandidatePhone,
            process.ResumeUrl,
            process.Status,
            process.CurrentStageId,
            process.Notes.Select(n => new CandidateNoteDetail(n.AuthorId, n.Text, n.IsPrivate, n.CreatedAt)).ToList(),
            process.Evaluations
                .Select(e => new CandidateEvaluationDetail(
                    e.StageId, e.EvaluatorId, ResolveEvaluatorEmail(e.EvaluatorId, evaluatorEmailsById), e.Score.Value, e.Comments, e.SubmittedAt))
                .ToList());

        return Result.Success(detail);
    }

    /// <summary>Falls back to the raw id only if the evaluator's User row is genuinely gone (shouldn't happen in practice) — a display glitch beats a thrown exception.</summary>
    private static string ResolveEvaluatorEmail(string evaluatorId, IReadOnlyDictionary<Guid, string> evaluatorEmailsById) =>
        Guid.TryParse(evaluatorId, out var id) && evaluatorEmailsById.TryGetValue(id, out var email) ? email : evaluatorId;
}
