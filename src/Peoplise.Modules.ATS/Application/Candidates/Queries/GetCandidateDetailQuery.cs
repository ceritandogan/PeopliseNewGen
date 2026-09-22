using MediatR;
using Microsoft.EntityFrameworkCore;
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

public sealed record CandidateEvaluationDetail(Guid StageId, string EvaluatorId, decimal Score, string? Comments, DateTimeOffset SubmittedAt);

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
            process.Evaluations.Select(e => new CandidateEvaluationDetail(e.StageId, e.EvaluatorId, e.Score.Value, e.Comments, e.SubmittedAt)).ToList());

        return Result.Success(detail);
    }
}
