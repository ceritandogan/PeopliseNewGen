using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Peoplise.Modules.ATS.Application.Candidates.Commands;
using Peoplise.Modules.ATS.Application.Candidates.Queries;
using Peoplise.Modules.ATS.Application.Positions.Queries;
using Peoplise.SharedKernel.MultiTenancy;
using static OpenIddict.Abstractions.OpenIddictConstants;

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

    /// <summary>
    /// The evaluator is whoever the access token's <c>sub</c> claim says is making the
    /// call — never taken from the request body — same reasoning as
    /// CasesController.SubmitScoring. Score-based stage rules (see WorkflowsController's
    /// rule endpoints) may silently auto-advance or auto-eliminate the candidate as a
    /// side effect of this call; the client sees that by re-fetching GetDetail, not from
    /// this response. No role check: same "no role administration exists yet" reasoning
    /// as the rest of this controller.
    /// </summary>
    [HttpPost("{candidateProcessId:guid}/evaluations")]
    public async Task<IActionResult> SubmitEvaluation(Guid candidateProcessId, SubmitEvaluationRequest request, CancellationToken cancellationToken)
    {
        var evaluatorId = User.FindFirstValue(Claims.Subject);
        if (string.IsNullOrEmpty(evaluatorId))
            return Unauthorized();

        var command = new SubmitEvaluationCommand(candidateProcessId, evaluatorId, request.Score, request.Comments);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// The author is whoever the access token's <c>email</c> claim says is making the
    /// call — never taken from the request body — same "never trust a client-supplied
    /// identity" reasoning as SubmitEvaluation, but reads the email claim rather than
    /// sub since AuthorId is shown to other reviewers as a human-readable label, not
    /// used as a stable id. No role check: same reasoning as the rest of this controller.
    /// </summary>
    [HttpPost("{candidateProcessId:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid candidateProcessId, AddNoteRequest request, CancellationToken cancellationToken)
    {
        var authorId = User.FindFirstValue(Claims.Email);
        if (string.IsNullOrEmpty(authorId))
            return Unauthorized();

        var command = new AddCandidateNoteCommand(candidateProcessId, authorId, request.Text, request.IsPrivate);
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

    /// <summary>No AuthorId here — deliberately: it's derived server-side from the caller's access token. See AddNote.</summary>
    public sealed record AddNoteRequest(string Text, bool IsPrivate);

    /// <summary>No EvaluatorId here — deliberately: it's derived server-side from the caller's access token, never accepted from the client. See SubmitEvaluation.</summary>
    public sealed record SubmitEvaluationRequest(decimal Score, string? Comments);
}
