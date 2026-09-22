using MediatR;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Queries;

public sealed record GetCaseBotProjectsForPositionQuery(Guid PositionId) : IRequest<Result<IReadOnlyList<CaseBotProjectSummaryDto>>>;

public sealed record CaseBotProjectSummaryDto(Guid Id, string Name, int RetakesAllowed, int RetentionPeriodDays);

public sealed class GetCaseBotProjectsForPositionQueryHandler
    : IRequestHandler<GetCaseBotProjectsForPositionQuery, Result<IReadOnlyList<CaseBotProjectSummaryDto>>>
{
    private readonly IRepository<CaseBotProject, CaseBotProjectId> _projects;

    public GetCaseBotProjectsForPositionQueryHandler(IRepository<CaseBotProject, CaseBotProjectId> projects)
    {
        _projects = projects;
    }

    public async Task<Result<IReadOnlyList<CaseBotProjectSummaryDto>>> Handle(
        GetCaseBotProjectsForPositionQuery request, CancellationToken cancellationToken)
    {
        var projects = await _projects.ListAsync(p => p.PositionId == request.PositionId, cancellationToken: cancellationToken);

        IReadOnlyList<CaseBotProjectSummaryDto> dtos = projects
            .Select(p => new CaseBotProjectSummaryDto(p.Id.Value, p.Name, p.RetakesAllowed, p.RetentionPeriodDays))
            .ToList();

        return Result.Success(dtos);
    }
}
