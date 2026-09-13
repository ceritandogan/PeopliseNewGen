using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Positions.Queries;

public sealed record GetPositionDashboardQuery(Guid PositionId) : IRequest<Result<PositionDashboard>>;

public sealed record PositionDashboard(
    Guid PositionId,
    string Title,
    int TotalApplicants,
    IReadOnlyDictionary<PipelineStatus, int> ApplicantsByStatus);

public sealed class GetPositionDashboardQueryHandler : IRequestHandler<GetPositionDashboardQuery, Result<PositionDashboard>>
{
    private readonly AppDbContext _context;

    public GetPositionDashboardQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PositionDashboard>> Handle(GetPositionDashboardQuery request, CancellationToken cancellationToken)
    {
        var positionId = PositionId.From(request.PositionId);

        var title = await _context.Set<Position>()
            .Where(p => p.Id == positionId)
            .Select(p => p.Title)
            .SingleOrDefaultAsync(cancellationToken);

        if (title is null)
            return Result.Failure<PositionDashboard>(Error.NotFound("Position.NotFound", $"No position '{request.PositionId}' was found."));

        var statusCounts = await _context.Set<CandidateProcess>()
            .Where(p => p.PositionId == positionId)
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byStatus = statusCounts.ToDictionary(x => x.Status, x => x.Count);

        return Result.Success(new PositionDashboard(request.PositionId, title, byStatus.Values.Sum(), byStatus));
    }
}
