using MediatR;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.Cases.Queries;

/// <summary>
/// Everything a reviewer needs to submit a score for one case, in a single round trip: the
/// steps the candidate actually answered (with the step's own question text and the
/// competencies it's related to, so a UI can narrow choices per step) and the full
/// competency list for the case's project (so a name can be shown instead of a raw id).
/// </summary>
public sealed record GetScoringContextQuery(Guid CaseId) : IRequest<Result<ScoringContextDto>>;

public sealed record ScoringContextDto(IReadOnlyList<ScorableStepDto> Steps, IReadOnlyList<CompetencyDto> Competencies);

public sealed record ScorableStepDto(Guid StepId, string Content, IReadOnlyList<Guid> RelatedCompetencyIds);

public sealed record CompetencyDto(Guid Id, string Name);

public sealed class GetScoringContextQueryHandler : IRequestHandler<GetScoringContextQuery, Result<ScoringContextDto>>
{
    private readonly IRepository<Case, CaseId> _cases;
    private readonly IRepository<CaseBotProject, CaseBotProjectId> _projects;

    public GetScoringContextQueryHandler(IRepository<Case, CaseId> cases, IRepository<CaseBotProject, CaseBotProjectId> projects)
    {
        _cases = cases;
        _projects = projects;
    }

    public async Task<Result<ScoringContextDto>> Handle(GetScoringContextQuery request, CancellationToken cancellationToken)
    {
        var @case = await _cases.GetByIdAsync(CaseId.From(request.CaseId), cancellationToken);
        if (@case is null)
            return Result.Failure<ScoringContextDto>(Error.NotFound("Case.NotFound", $"No case '{request.CaseId}' was found."));

        var project = await _projects.GetByIdAsync(@case.CaseBotProjectId, cancellationToken);
        if (project is null)
            return Result.Failure<ScoringContextDto>(Error.NotFound("CaseBotProject.NotFound", "The case's project could not be found."));

        var allSteps = project.Flows.SelectMany(f => f.Steps).ToList();

        var scorableSteps = @case.StepConversations
            .Select(sc => allSteps.FirstOrDefault(s => s.Id == sc.StepId))
            .Where(step => step is not null)
            .Select(step => new ScorableStepDto(step!.Id, step.Content, step.RelatedCompetencyIds.ToList()))
            .ToList();

        var competencies = project.Competencies.Select(c => new CompetencyDto(c.Id, c.Name)).ToList();

        return Result.Success(new ScoringContextDto(scorableSteps, competencies));
    }
}
