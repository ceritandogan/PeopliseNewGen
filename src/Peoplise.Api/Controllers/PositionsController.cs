using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Peoplise.Modules.ATS.Application.Candidates.Queries;
using Peoplise.Modules.ATS.Application.Positions.Commands;
using Peoplise.Modules.ATS.Application.Positions.Queries;
using Peoplise.Modules.ATS.Domain.ValueObjects;

namespace Peoplise.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/positions")]
public sealed class PositionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PositionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePositionCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(new { id = result.Value }) : result.ToActionResult(this);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPositionsListQuery(page, pageSize), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{positionId:guid}/dashboard")]
    public async Task<IActionResult> GetDashboard(Guid positionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPositionDashboardQuery(positionId), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{positionId:guid}/candidates")]
    public async Task<IActionResult> GetCandidates(
        Guid positionId,
        [FromQuery] PipelineStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCandidatePipelineQuery(positionId, status, page, pageSize), cancellationToken);
        return result.ToActionResult(this);
    }
}
