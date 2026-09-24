using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Peoplise.Api.Filters;
using Peoplise.Api.Services;
using Peoplise.Infrastructure.Security;
using Peoplise.Modules.ATS.Application.Positions.Queries;
using Peoplise.Modules.VideoInterview.Application.Cases.Commands;
using Peoplise.Modules.VideoInterview.Application.Cases.Queries;
using Peoplise.SharedKernel.MultiTenancy;
using static OpenIddict.Abstractions.OpenIddictConstants;

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
    private readonly ICandidateLinkMailer _linkMailer;
    private readonly ILogger<CasesController> _logger;

    public CasesController(
        IMediator mediator, ICandidateResourceTokenService candidateTokens, ICandidateLinkMailer linkMailer, ILogger<CasesController> logger)
    {
        _mediator = mediator;
        _candidateTokens = candidateTokens;
        _linkMailer = linkMailer;
        _logger = logger;
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

        // Best-effort — see ConversationsController.Start's identical reasoning.
        try
        {
            await _linkMailer.SendCaseLinkAsync(command.PositionId, command.CandidateId, result.Value.CaseId, token, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to email case link for candidate {CandidateId}.", command.CandidateId);
        }

        return Ok(new StartCandidateCaseResponse(result.Value, token));
    }

    /// <summary>
    /// Panel-facing resend — see ConversationsController.ResendLink's identical reasoning
    /// (must-succeed, not best-effort).
    /// </summary>
    [HttpPost("resend-link")]
    public async Task<IActionResult> ResendLink([FromBody] ResendCaseLinkRequest request, CancellationToken cancellationToken)
    {
        var lookup = await _mediator.Send(new GetCaseForCandidateQuery(request.CandidateId, request.PositionId), cancellationToken);
        if (lookup.IsFailure)
            return lookup.ToActionResult(this);
        if (lookup.Value is not { } caseId)
            return NotFound();

        var token = _candidateTokens.Issue(
            CandidateResourceType.Case, caseId, DateTimeOffset.UtcNow.Add(CandidateTokenLifetime));

        await _linkMailer.SendCaseLinkAsync(request.PositionId, request.CandidateId, caseId, token, cancellationToken);

        return Ok();
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

    /// <summary>
    /// Panel-facing. No role check — this app has no role administration yet (a single
    /// hardcoded seed user), so gating on a role here would gate nothing real; see ADR
    /// 0005's neighboring design notes. Recomputed on every call, never cached.
    /// </summary>
    [HttpGet("{caseId:guid}/report")]
    public async Task<IActionResult> GetReport(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCaseReportQuery(caseId), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// The reviewer is whoever the access token's <c>sub</c> claim says is making the
    /// call — never taken from the request body — so a score can't be submitted under
    /// someone else's name. No role check: same "no role administration exists yet"
    /// reasoning as the rest of this controller.
    /// </summary>
    [HttpPost("{caseId:guid}/scorings")]
    public async Task<IActionResult> SubmitScoring(Guid caseId, SubmitScoringRequest request, CancellationToken cancellationToken)
    {
        var reviewerId = User.FindFirstValue(Claims.Subject);
        if (string.IsNullOrEmpty(reviewerId))
            return Unauthorized();

        var command = new SubmitReviewerScoringCommand(caseId, reviewerId, request.StepId, request.CompetencyId, request.Score, request.Notes);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Everything a reviewer needs to fill out the scoring form for this case — see GetScoringContextQuery's remarks.</summary>
    [HttpGet("{caseId:guid}/scoring-context")]
    public async Task<IActionResult> GetScoringContext(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetScoringContextQuery(caseId), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Panel-facing, HR pastes in code manually — there's no candidate-facing
    /// "SoftwareDevelopmentQuestion" step UI yet, so <paramref name="request"/>'s
    /// <c>StepId</c> is a fresh id the caller mints, not a real step from this case's
    /// project. No role check, same reasoning as the rest of this controller. Requires
    /// AI:Anthropic:ApiKey to be configured (see scripts/setup-anthropic-key.sh) — an
    /// unconfigured IAIProvider throws, which the global exception middleware turns into
    /// a 500, deliberately (see NotConfiguredAIProvider's own remarks).
    /// </summary>
    [HttpPost("{caseId:guid}/code-review")]
    public async Task<IActionResult> RequestCodeReview(Guid caseId, RequestCodeReviewRequest request, CancellationToken cancellationToken)
    {
        var command = new RequestAICodeReviewCommand(caseId, request.StepId, request.Question, request.CandidateCode);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// HR/panel-only lookup: given a candidate + position (what CandidateDetailPage
    /// already has), finds the matching case, if the candidate has started one.
    /// `caseId: null` is a normal, successful "hasn't started their video interview yet"
    /// answer, not a 404 — see GetCaseForCandidateQuery's remarks.
    /// </summary>
    [HttpGet("by-candidate")]
    public async Task<IActionResult> GetForCandidate([FromQuery] Guid candidateId, [FromQuery] Guid positionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCaseForCandidateQuery(candidateId, positionId), cancellationToken);
        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(new CaseForCandidateResponse(result.Value));
    }
}

public sealed record WithdrawCaseConsentRequest(string? Reason);

/// <summary>No ReviewerId here — deliberately: it's derived server-side from the caller's access token, never accepted from the client. See CasesController.SubmitScoring.</summary>
public sealed record SubmitScoringRequest(Guid StepId, Guid CompetencyId, int Score, string? Notes);

/// <summary>StepId is caller-minted — see CasesController.RequestCodeReview's remarks.</summary>
public sealed record RequestCodeReviewRequest(Guid StepId, string Question, string CandidateCode);

/// <summary>Wraps StartCandidateCaseResult with the candidate access token — see ADR 0004. Every later request for this case must present this token in the X-Candidate-Token header.</summary>
public sealed record StartCandidateCaseResponse(StartCandidateCaseResult Case, string CandidateToken);

/// <summary>caseId is null when the candidate hasn't started their video interview yet — not an error.</summary>
public sealed record CaseForCandidateResponse(Guid? CaseId);

public sealed record ResendCaseLinkRequest(Guid CandidateId, Guid PositionId);
