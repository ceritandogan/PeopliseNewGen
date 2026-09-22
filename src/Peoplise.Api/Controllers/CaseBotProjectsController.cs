using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Commands;
using Peoplise.Modules.VideoInterview.Application.Cases.Queries;

namespace Peoplise.Api.Controllers;

/// <summary>
/// Deliberately doesn't include project creation (<c>CreateCaseBotProjectCommand</c>
/// stays unwired) — that's a separate, longer-standing gap, not this controller's scope.
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
}

public sealed record AddCompetencyRequest(string Name, string? Description);
