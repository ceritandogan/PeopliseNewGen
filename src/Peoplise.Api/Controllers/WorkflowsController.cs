using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Peoplise.Modules.ATS.Application.Workflows.Commands;
using Peoplise.Modules.ATS.Application.Workflows.Queries;
using Peoplise.Modules.ATS.Domain.ValueObjects;

namespace Peoplise.Api.Controllers;

/// <summary>
/// No role check — same "no role administration exists yet" reasoning as every other
/// controller in this app.
/// </summary>
[ApiController]
[Authorize]
[Route("api/workflows")]
public sealed class WorkflowsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkflowsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>A position points at its workflow, not the reverse — see GetWorkflowStagesForPositionQuery's remarks.</summary>
    [HttpGet]
    public async Task<IActionResult> GetForPosition([FromQuery] Guid positionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWorkflowStagesForPositionQuery(positionId), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{workflowDefinitionId:guid}/stages")]
    public async Task<IActionResult> AddStage(Guid workflowDefinitionId, AddStageRequest request, CancellationToken cancellationToken)
    {
        var command = new AddStageToWorkflowCommand(workflowDefinitionId, request.Name, request.Type, request.Order);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Full-sequence replacement, not an incremental move — see ReorderWorkflowStagesCommand's remarks.</summary>
    [HttpPut("{workflowDefinitionId:guid}/stages/reorder")]
    public async Task<IActionResult> ReorderStages(Guid workflowDefinitionId, ReorderStagesRequest request, CancellationToken cancellationToken)
    {
        var command = new ReorderWorkflowStagesCommand(workflowDefinitionId, request.OrderedStageIds);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>409 WorkflowDefinition.StageInUse if a candidate is currently on this stage — see RemoveWorkflowStageCommand's remarks.</summary>
    [HttpDelete("{workflowDefinitionId:guid}/stages/{stageId:guid}")]
    public async Task<IActionResult> RemoveStage(Guid workflowDefinitionId, Guid stageId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveWorkflowStageCommand(workflowDefinitionId, stageId), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{workflowDefinitionId:guid}/stages/{stageId:guid}/rules")]
    public async Task<IActionResult> AddRule(Guid workflowDefinitionId, Guid stageId, AddStageRuleRequest request, CancellationToken cancellationToken)
    {
        var command = new AddStageRuleCommand(workflowDefinitionId, stageId, request.Type, request.Threshold, request.DelayDays);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{workflowDefinitionId:guid}/stages/{stageId:guid}/rules/{ruleId:guid}")]
    public async Task<IActionResult> RemoveRule(Guid workflowDefinitionId, Guid stageId, Guid ruleId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveStageRuleCommand(workflowDefinitionId, stageId, ruleId), cancellationToken);
        return result.ToActionResult(this);
    }
}

/// <summary>Order is computed client-side (append to the end, from the stage list the UI already has loaded) rather than asked of the user — see RequestCodeReviewRequest's StepId for the same reasoning applied elsewhere.</summary>
public sealed record AddStageRequest(string Name, StageType Type, int Order);

public sealed record ReorderStagesRequest(IReadOnlyList<Guid> OrderedStageIds);

/// <summary>Threshold is required for AdvanceIfScoreAtLeast/EliminateIfScoreBelow, DelayDays for ActivateAfterDelay — see AddStageRuleCommandValidator.</summary>
public sealed record AddStageRuleRequest(StageRuleType Type, decimal? Threshold, int? DelayDays);
