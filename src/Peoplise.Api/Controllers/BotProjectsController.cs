using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Peoplise.Modules.ATS.Application.Positions.Queries;
using Peoplise.Modules.HrBot.Application.BotProjects.Commands;
using Peoplise.Modules.HrBot.Application.BotProjects.Queries;

namespace Peoplise.Api.Controllers;

/// <summary>
/// No role check, same "no role administration exists yet" reasoning as CasesController.
/// </summary>
[ApiController]
[Authorize]
[Route("api/bot-projects")]
public sealed class BotProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BotProjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Validates PositionId server-side (tenant-scoped — see ValidatePositionExistsQuery)
    /// before creating: only Guids cross the module boundary here, never ATS's own
    /// Position/PositionId types, matching BotProject's own "modules don't share domain
    /// types" doc comment.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateBotProjectCommand command, CancellationToken cancellationToken)
    {
        var positionCheck = await _mediator.Send(new ValidatePositionExistsQuery(command.PositionId), cancellationToken);
        if (positionCheck.IsFailure)
            return positionCheck.ToActionResult(this);

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet]
    public async Task<IActionResult> ListForPosition([FromQuery] Guid positionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetBotProjectsForPositionQuery(positionId), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{botProjectId:guid}/flows")]
    public async Task<IActionResult> GetFlows(Guid botProjectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFlowsForBotProjectQuery(botProjectId), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{botProjectId:guid}/flows")]
    public async Task<IActionResult> AddFlow(Guid botProjectId, AddBotFlowRequest request, CancellationToken cancellationToken)
    {
        var command = new AddFlowCommand(botProjectId, request.Name, request.IsDefault);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{botProjectId:guid}/flows/{flowId:guid}/steps")]
    public async Task<IActionResult> AddStep(Guid botProjectId, Guid flowId, AddBotStepRequest request, CancellationToken cancellationToken)
    {
        var command = new AddStepCommand(
            botProjectId, flowId, request.Type, request.Content, request.Order,
            request.QuickReplyOptions, request.CaptureVariableKey, request.IsFinalStep, request.IsScreenOut);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{botProjectId:guid}/flows/{flowId:guid}/steps/{stepId:guid}/routes")]
    public async Task<IActionResult> AddStepRoute(
        Guid botProjectId, Guid flowId, Guid stepId, AddBotStepRouteRequest request, CancellationToken cancellationToken)
    {
        var command = new AddStepRouteCommand(
            botProjectId, flowId, stepId, request.ConditionType, request.Keywords, request.RouteType,
            request.TargetFlowId, request.TargetStepId);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{botProjectId:guid}/flows/{flowId:guid}/steps/{stepId:guid}/routes/{routeId:guid}")]
    public async Task<IActionResult> RemoveStepRoute(
        Guid botProjectId, Guid flowId, Guid stepId, Guid routeId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveStepRouteCommand(botProjectId, flowId, stepId, routeId), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{botProjectId:guid}/variables")]
    public async Task<IActionResult> GetVariables(Guid botProjectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetVariablesForProjectQuery(botProjectId), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{botProjectId:guid}/variables")]
    public async Task<IActionResult> AddVariable(Guid botProjectId, AddVariableRequest request, CancellationToken cancellationToken)
    {
        var command = new AddVariableCommand(botProjectId, request.Key, request.Description);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}

public sealed record AddBotFlowRequest(string Name, bool IsDefault);

public sealed record AddVariableRequest(string Key, string? Description);

public sealed record AddBotStepRequest(
    Peoplise.Modules.HrBot.Domain.ValueObjects.StepType Type,
    string Content,
    int Order,
    IReadOnlyList<string>? QuickReplyOptions,
    string? CaptureVariableKey,
    bool IsFinalStep,
    bool IsScreenOut);

/// <summary>Target fields are validated by RouteType server-side — see AddStepRouteCommandValidator.</summary>
public sealed record AddBotStepRouteRequest(
    Peoplise.Modules.HrBot.Domain.ValueObjects.ConditionType ConditionType,
    IReadOnlyList<string> Keywords,
    Peoplise.Modules.HrBot.Domain.ValueObjects.StepRouteType RouteType,
    Guid? TargetFlowId,
    Guid? TargetStepId);
