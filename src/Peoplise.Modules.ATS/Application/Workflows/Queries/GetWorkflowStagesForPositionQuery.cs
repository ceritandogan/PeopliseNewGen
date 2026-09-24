using MediatR;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Workflows.Queries;

/// <summary>
/// A position points at its <c>WorkflowDefinition</c> (not the reverse — unlike
/// CaseBotProject, WorkflowDefinition carries no PositionId of its own, since it's a
/// reusable, nameable template a position merely references), so this resolves
/// Position → WorkflowDefinitionId → the workflow's own stages in two lookups.
/// </summary>
public sealed record GetWorkflowStagesForPositionQuery(Guid PositionId) : IRequest<Result<WorkflowStagesDto>>;

public sealed record WorkflowStagesDto(Guid WorkflowDefinitionId, IReadOnlyList<WorkflowStageDto> Stages);

public sealed record WorkflowStageDto(Guid Id, string Name, StageType Type, int Order, IReadOnlyList<StageRuleDto> Rules);

public sealed record StageRuleDto(Guid Id, StageRuleType Type, decimal? Threshold, int? DelayDays);

public sealed class GetWorkflowStagesForPositionQueryHandler
    : IRequestHandler<GetWorkflowStagesForPositionQuery, Result<WorkflowStagesDto>>
{
    private readonly IRepository<Position, PositionId> _positions;
    private readonly IRepository<WorkflowDefinition, WorkflowDefinitionId> _workflows;

    public GetWorkflowStagesForPositionQueryHandler(
        IRepository<Position, PositionId> positions, IRepository<WorkflowDefinition, WorkflowDefinitionId> workflows)
    {
        _positions = positions;
        _workflows = workflows;
    }

    public async Task<Result<WorkflowStagesDto>> Handle(GetWorkflowStagesForPositionQuery request, CancellationToken cancellationToken)
    {
        var position = await _positions.GetByIdAsync(PositionId.From(request.PositionId), cancellationToken);
        if (position is null)
            return Result.Failure<WorkflowStagesDto>(Error.NotFound("Position.NotFound", $"No position '{request.PositionId}' was found."));

        if (position.WorkflowDefinitionId is null)
            return Result.Failure<WorkflowStagesDto>(Error.Conflict("Position.NoWorkflow", "This position has no workflow assigned yet."));

        var workflow = await _workflows.GetByIdAsync(position.WorkflowDefinitionId, cancellationToken);
        if (workflow is null)
        {
            return Result.Failure<WorkflowStagesDto>(Error.NotFound(
                "WorkflowDefinition.NotFound", $"No workflow definition '{position.WorkflowDefinitionId.Value}' was found."));
        }

        var stages = workflow.Stages
            .Select(s => new WorkflowStageDto(
                s.Id, s.Name, s.Type, s.Order,
                s.Rules.Select(r => new StageRuleDto(r.Id, r.Type, r.Threshold, r.DelayDays)).ToList()))
            .ToList();
        return Result.Success(new WorkflowStagesDto(workflow.Id.Value, stages));
    }
}
