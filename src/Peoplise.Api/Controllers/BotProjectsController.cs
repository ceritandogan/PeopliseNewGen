using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Peoplise.Modules.ATS.Application.Positions.Queries;
using Peoplise.Modules.HrBot.Application.BotProjects.Commands;

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
}
