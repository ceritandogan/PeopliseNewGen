using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Pagination;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Candidates.Queries;

/// <summary>Filterable list of a position's candidates — the Kanban pipeline view.</summary>
public sealed record GetCandidatePipelineQuery(
    Guid PositionId,
    PipelineStatus? Status,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<CandidatePipelineItem>>>;

public sealed record CandidatePipelineItem(
    Guid CandidateProcessId,
    string CandidateName,
    string CandidateEmail,
    PipelineStatus Status,
    Guid? CurrentStageId);

public sealed class GetCandidatePipelineQueryHandler
    : IRequestHandler<GetCandidatePipelineQuery, Result<PagedResult<CandidatePipelineItem>>>
{
    private readonly AppDbContext _context;

    public GetCandidatePipelineQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<CandidatePipelineItem>>> Handle(
        GetCandidatePipelineQuery request, CancellationToken cancellationToken)
    {
        var positionId = PositionId.From(request.PositionId);
        var pagedRequest = new PagedRequest(request.Page, request.PageSize);

        var query = _context.Set<CandidateProcess>().Where(p => p.PositionId == positionId);
        if (request.Status is not null)
            query = query.Where(p => p.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        // Project only the scalar columns this list actually needs — not the full
        // aggregate with its Notes/Evaluations/CompletedStages collections. Keep
        // strongly-typed-id properties (CandidateProcessId, a value-converted type) as
        // whole values here rather than accessing `.Value` inside the query: EF Core's
        // SQL translator isn't guaranteed to unwrap a member access on top of a
        // converted property, so the unwrap happens after materialization instead.
        var rows = await query
            .OrderBy(p => p.CreatedAt)
            .Skip(pagedRequest.Skip)
            .Take(pagedRequest.PageSize)
            .Select(p => new { p.Id, p.CandidateName, p.CandidateEmail, p.Status, p.CurrentStageId })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new CandidatePipelineItem(r.Id.Value, r.CandidateName, r.CandidateEmail, r.Status, r.CurrentStageId))
            .ToList();

        return Result.Success(new PagedResult<CandidatePipelineItem>(items, pagedRequest.Page, pagedRequest.PageSize, totalCount));
    }
}
