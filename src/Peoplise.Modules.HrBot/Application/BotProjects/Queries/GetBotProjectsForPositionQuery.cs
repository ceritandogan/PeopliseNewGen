using MediatR;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.HrBot.Application.BotProjects.Queries;

public sealed record GetBotProjectsForPositionQuery(Guid PositionId) : IRequest<Result<IReadOnlyList<BotProjectSummaryDto>>>;

public sealed record BotProjectSummaryDto(Guid Id, string Name, int RetentionPeriodDays);

public sealed class GetBotProjectsForPositionQueryHandler
    : IRequestHandler<GetBotProjectsForPositionQuery, Result<IReadOnlyList<BotProjectSummaryDto>>>
{
    private readonly IRepository<BotProject, BotProjectId> _projects;

    public GetBotProjectsForPositionQueryHandler(IRepository<BotProject, BotProjectId> projects)
    {
        _projects = projects;
    }

    public async Task<Result<IReadOnlyList<BotProjectSummaryDto>>> Handle(
        GetBotProjectsForPositionQuery request, CancellationToken cancellationToken)
    {
        var projects = await _projects.ListAsync(p => p.PositionId == request.PositionId, cancellationToken: cancellationToken);

        IReadOnlyList<BotProjectSummaryDto> dtos = projects
            .Select(p => new BotProjectSummaryDto(p.Id.Value, p.Name, p.RetentionPeriodDays))
            .ToList();

        return Result.Success(dtos);
    }
}
