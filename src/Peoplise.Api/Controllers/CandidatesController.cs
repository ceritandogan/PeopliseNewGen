using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Peoplise.Modules.ATS.Application.Candidates.Commands;
using Peoplise.Modules.ATS.Application.Candidates.Queries;
using Peoplise.Modules.ATS.Application.Positions.Queries;
using Peoplise.SharedKernel.MultiTenancy;

namespace Peoplise.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/candidates")]
public sealed class CandidatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CandidatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{candidateProcessId:guid}")]
    public async Task<IActionResult> GetDetail(Guid candidateProcessId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCandidateDetailQuery(candidateProcessId), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// The one deliberately anonymous write in this module (see Q9/Q10 of the plan this
    /// implements): a candidate applying has no session, so there's no tenant claim to
    /// resolve from. Resolves the tenant from the position being applied to instead
    /// (<see cref="ResolvePositionTenantQuery"/>) and sets it as an
    /// <see cref="AmbientTenantOverride"/> before dispatching the actual write.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("apply")]
    public async Task<IActionResult> Apply(SubmitCandidateApplicationCommand command, CancellationToken cancellationToken)
    {
        var tenantResult = await _mediator.Send(new ResolvePositionTenantQuery(command.PositionId), cancellationToken);
        if (tenantResult.IsFailure)
            return tenantResult.ToActionResult(this);

        using var _ = AmbientTenantOverride.Begin(TenantId.From(tenantResult.Value));

        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(new { id = result.Value }) : result.ToActionResult(this);
    }

    [HttpPost("{candidateProcessId:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid candidateProcessId, AddNoteRequest request, CancellationToken cancellationToken)
    {
        var command = new AddCandidateNoteCommand(candidateProcessId, request.AuthorId, request.Text, request.IsPrivate);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Panel-facing bulk name lookup for raw candidate ids — see GetCandidateNamesQuery's remarks.</summary>
    [HttpGet("names")]
    public async Task<IActionResult> GetNames([FromQuery] Guid positionId, [FromQuery] Guid[] candidateIds, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCandidateNamesQuery(positionId, candidateIds), cancellationToken);
        return result.ToActionResult(this);
    }

    public sealed record AddNoteRequest(string AuthorId, string Text, bool IsPrivate);
}
