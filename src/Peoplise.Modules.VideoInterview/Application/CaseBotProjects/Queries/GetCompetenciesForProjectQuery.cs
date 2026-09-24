using MediatR;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Queries;

/// <summary>
/// Column headers for a competency comparison view — id+name pairs, independent of any
/// one case. Also carries each competency's rubric (Levels/Indicators) for the
/// competency-authoring page; existing consumers (comparison headers, the reviewer
/// scoring dropdown) only ever read Id/Name and ignore the rest.
/// </summary>
public sealed record GetCompetenciesForProjectQuery(Guid CaseBotProjectId) : IRequest<Result<IReadOnlyList<CompetencySummaryDto>>>;

public sealed record CompetencySummaryDto(
    Guid Id, string Name, string? Description, IReadOnlyList<CompetencyLevelDto> Levels, IReadOnlyList<CompetencyIndicatorDto> Indicators);

public sealed record CompetencyLevelDto(Guid Id, int Level, string Description);

public sealed record CompetencyIndicatorDto(Guid Id, string Description);

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

        IReadOnlyList<CompetencySummaryDto> competencies = project.Competencies
            .Select(c => new CompetencySummaryDto(
                c.Id,
                c.Name,
                c.Description,
                c.Levels.OrderBy(l => l.Level).Select(l => new CompetencyLevelDto(l.Id, l.Level, l.Description)).ToList(),
                c.Indicators.Select(i => new CompetencyIndicatorDto(i.Id, i.Description)).ToList()))
            .ToList();

        return Result.Success(competencies);
    }
}
