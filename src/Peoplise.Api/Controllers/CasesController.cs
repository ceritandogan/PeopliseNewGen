using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Peoplise.Api.Filters;
using Peoplise.Infrastructure.Security;
using Peoplise.Modules.ATS.Application.Positions.Queries;
using Peoplise.Modules.VideoInterview.Application.Cases.Commands;
using Peoplise.Modules.VideoInterview.Application.Cases.Queries;
using Peoplise.SharedKernel.MultiTenancy;

namespace Peoplise.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cases")]
public sealed class CasesController : ControllerBase
{
    /// <summary>How long a candidate's link stays usable after a case starts. See ADR 0004.</summary>
    private static readonly TimeSpan CandidateTokenLifetime = TimeSpan.FromDays(30);

    private readonly IMediator _mediator;
    private readonly ICandidateResourceTokenService _candidateTokens;

    public CasesController(IMediator mediator, ICandidateResourceTokenService candidateTokens)
    {
        _mediator = mediator;
        _candidateTokens = candidateTokens;
    }

    /// <summary>
    /// Anonymous, same reasoning as <see cref="ConversationsController"/>'s Start: a
    /// candidate taking the video interview has no session, so there's no tenant claim
    /// to resolve from. Resolves it from the position instead. Issues the candidate
    /// access token here, same reasoning as <see cref="ConversationsController.Start"/>.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Start(StartCandidateCaseCommand command, CancellationToken cancellationToken)
    {
        var tenantResult = await _mediator.Send(new ResolvePositionTenantQuery(command.PositionId), cancellationToken);
        if (tenantResult.IsFailure)
            return tenantResult.ToActionResult(this);

        using var _ = AmbientTenantOverride.Begin(TenantId.From(tenantResult.Value));

        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailure)
            return result.ToActionResult(this);

        var token = _candidateTokens.Issue(
            CandidateResourceType.Case, result.Value.CaseId, DateTimeOffset.UtcNow.Add(CandidateTokenLifetime));

        return Ok(new StartCandidateCaseResponse(result.Value, token));
    }

    [AllowAnonymous]
    [RequireCandidateResourceToken(CandidateResourceType.Case, "caseId")]
    [HttpPost("{caseId:guid}/video-answers")]
    public async Task<IActionResult> SubmitVideoAnswer(
        Guid caseId, [FromForm] Guid stepId, IFormFile video, CancellationToken cancellationToken)
    {
        var tenantResult = await _mediator.Send(new ResolveCaseTenantQuery(caseId), cancellationToken);
        if (tenantResult.IsFailure)
            return tenantResult.ToActionResult(this);

        using var _ = AmbientTenantOverride.Begin(TenantId.From(tenantResult.Value));

        await using var stream = video.OpenReadStream();
        var command = new SubmitVideoAnswerCommand(caseId, stepId, stream, video.FileName, video.ContentType);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// KVKK: an HR/panel user acting on a candidate's consent-withdrawal request
    /// (received some other way — email, a form). Authenticated like the rest of this
    /// controller's non-candidate-facing routes. Not exposed to the candidate app itself
    /// even now that it has its own access token (see ADR 0004) — that token proves
    /// "this is the intended candidate," not "this candidate is authorized to trigger a
    /// KVKK data-deletion workflow," which stays an HR-mediated action.
    /// </summary>
    [HttpPost("{caseId:guid}/withdraw-consent")]
    public async Task<IActionResult> WithdrawConsent(Guid caseId, WithdrawCaseConsentRequest request, CancellationToken cancellationToken)
    {
        var command = new RequestDataDeletionCommand(caseId, DataDeletionReason.ConsentWithdrawn, request.Reason);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}

public sealed record WithdrawCaseConsentRequest(string? Reason);

/// <summary>Wraps StartCandidateCaseResult with the candidate access token — see ADR 0004. Every later request for this case must present this token in the X-Candidate-Token header.</summary>
public sealed record StartCandidateCaseResponse(StartCandidateCaseResult Case, string CandidateToken);
