using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.SharedKernel.Pagination;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Positions.Queries;

/// <summary>Lists every position for the tenant, most recently created last. There's no position lifecycle/status yet, so this is unfiltered — see CONTEXT.md.</summary>
public sealed record GetPositionsListQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedResult<PositionListItem>>>;

public sealed record PositionListItem(Guid PositionId, string Title, string Department, string City, string Country, int ApplicantCount);

public sealed class GetPositionsListQueryHandler : IRequestHandler<GetPositionsListQuery, Result<PagedResult<PositionListItem>>>
{
    private readonly AppDbContext _context;

    public GetPositionsListQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<PositionListItem>>> Handle(GetPositionsListQuery request, CancellationToken cancellationToken)
    {
        var pagedRequest = new PagedRequest(request.Page, request.PageSize);

        var query = _context.Set<Position>().OrderBy(p => p.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(pagedRequest.Skip)
            .Take(pagedRequest.PageSize)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Department,
                p.City,
                p.Country,
                // Correlated subquery, not a per-position round trip: EF Core folds this
                // into the same SQL statement as the paged position list above.
                ApplicantCount = _context.Set<CandidateProcess>().Count(c => c.PositionId == p.Id),
            })
            .ToListAsync(cancellationToken);

        var mapped = items
            .Select(p => new PositionListItem(p.Id.Value, p.Title, p.Department, p.City, p.Country, p.ApplicantCount))
            .ToList();

        return Result.Success(new PagedResult<PositionListItem>(mapped, pagedRequest.Page, pagedRequest.PageSize, totalCount));
    }
}
