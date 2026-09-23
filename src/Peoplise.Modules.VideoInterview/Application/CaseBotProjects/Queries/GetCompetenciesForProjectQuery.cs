using MediatR;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Queries;

/// <summary>Column headers for a competency comparison view — id+name pairs, independent of any one case.</summary>
public sealed record GetCompetenciesForProjectQuery(Guid CaseBotProjectId) : IRequest<Result<IReadOnlyList<CompetencySummaryDto>>>;

public sealed record CompetencySummaryDto(Guid Id, string Name);

public sealed class GetCompetenciesForProjectQueryHandler
    : IRequestHandler<GetCompetenciesForProjectQuery, Result<IReadOnlyList<CompetencySummaryDto>>>
{
    private readonly IRepository<CaseBotProject, CaseBotProjectId> _projects;

    public GetCompetenciesForProjectQueryHandler(IRepository<CaseBotProject, CaseBotProjectId> projects)
    {
        _projects = projects;
    }

    public async Task<Result<IReadOnlyList<CompetencySummaryDto>>> Handle(
        GetCompetenciesForProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(CaseBotProjectId.From(request.CaseBotProjectId), cancellationToken);
        if (project is null)
            return Result.Failure<IReadOnlyList<CompetencySummaryDto>>(
                Error.NotFound("CaseBotProject.NotFound", $"No project '{request.CaseBotProjectId}' was found."));

        IReadOnlyList<CompetencySummaryDto> competencies = project.Competencies.Select(c => new CompetencySummaryDto(c.Id, c.Name)).ToList();
        return Result.Success(competencies);
    }
}
