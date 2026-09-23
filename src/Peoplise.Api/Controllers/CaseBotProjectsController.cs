using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Peoplise.Modules.ATS.Application.Positions.Queries;
using Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Commands;
using Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Queries;
using Peoplise.Modules.VideoInterview.Application.Cases.Queries;

namespace Peoplise.Api.Controllers;

/// <summary>
/// No role check, same "no role administration exists yet" reasoning as CasesController.
/// </summary>
[ApiController]
[Authorize]
[Route("api/case-bot-projects")]
public sealed class CaseBotProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CaseBotProjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Validates PositionId server-side (tenant-scoped — see ValidatePositionExistsQuery)
    /// before creating: only Guids cross the module boundary here, never ATS's own
    /// Position/PositionId types, matching CaseBotProject's own "modules don't share
    /// domain types" doc comment.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateCaseBotProjectCommand command, CancellationToken cancellationToken)
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
        var result = await _mediator.Send(new GetCaseBotProjectsForPositionQuery(positionId), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{caseBotProjectId:guid}/competencies")]
    public async Task<IActionResult> AddCompetency(Guid caseBotProjectId, AddCompetencyRequest request, CancellationToken cancellationToken)
    {
        var command = new AddCompetencyCommand(caseBotProjectId, request.Name, request.Description);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{caseBotProjectId:guid}/comparison")]
    public async Task<IActionResult> GetComparison(Guid caseBotProjectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCandidateComparisonQuery(caseBotProjectId), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Column headers (id+name) for the comparison view — see GetCompetenciesForProjectQuery's remarks.</summary>
    [HttpGet("{caseBotProjectId:guid}/competencies")]
    public async Task<IActionResult> GetCompetencies(Guid caseBotProjectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCompetenciesForProjectQuery(caseBotProjectId), cancellationToken);
        return result.ToActionResult(this);
    }
}

public sealed record AddCompetencyRequest(string Name, string? Description);
