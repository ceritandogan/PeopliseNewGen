using MediatR;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Queries;

/// <summary>The authored flow/step structure for one project — nothing returned this before (GetScoringContextQuery only exposes a narrow, already-answered subset of step data).</summary>
public sealed record GetFlowsForProjectQuery(Guid CaseBotProjectId) : IRequest<Result<IReadOnlyList<FlowDto>>>;

public sealed record FlowDto(Guid Id, string Name, bool IsDefault, IReadOnlyList<FlowStepDto> Steps);

public sealed record FlowStepDto(
    Guid Id,
    StepType Type,
    string Content,
    int Order,
    int? PreparationTimeSeconds,
    int? RecordingTimeSeconds,
    IReadOnlyList<Guid> RelatedCompetencyIds);

public sealed class GetFlowsForProjectQueryHandler : IRequestHandler<GetFlowsForProjectQuery, Result<IReadOnlyList<FlowDto>>>
{
    private readonly IRepository<CaseBotProject, CaseBotProjectId> _projects;

    public GetFlowsForProjectQueryHandler(IRepository<CaseBotProject, CaseBotProjectId> projects)
    {
        _projects = projects;
    }

    public async Task<Result<IReadOnlyList<FlowDto>>> Handle(GetFlowsForProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(CaseBotProjectId.From(request.CaseBotProjectId), cancellationToken);
        if (project is null)
            return Result.Failure<IReadOnlyList<FlowDto>>(Error.NotFound("CaseBotProject.NotFound", $"No project '{request.CaseBotProjectId}' was found."));

        IReadOnlyList<FlowDto> flows = project.Flows
            .Select(f => new FlowDto(
                f.Id,
                f.Name,
                f.IsDefault,
                f.Steps.Select(s => new FlowStepDto(
                    s.Id, s.Type, s.Content, s.Order, s.PreparationTimeSeconds, s.RecordingTimeSeconds, s.RelatedCompetencyIds.ToList())).ToList()))
            .ToList();

        return Result.Success(flows);
    }
}
