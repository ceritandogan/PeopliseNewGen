using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Peoplise.Api.Filters;
using Peoplise.Api.Services;
using Peoplise.Infrastructure.Security;
using Peoplise.Modules.ATS.Application.Positions.Queries;
using Peoplise.Modules.HrBot.Application.Conversations.Commands;
using Peoplise.Modules.HrBot.Application.Conversations.Queries;
using Peoplise.SharedKernel.MultiTenancy;

namespace Peoplise.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/conversations")]
public sealed class ConversationsController : ControllerBase
{
    /// <summary>How long a candidate's link stays usable after a conversation starts. See ADR 0004.</summary>
    private static readonly TimeSpan CandidateTokenLifetime = TimeSpan.FromDays(30);

    private readonly IMediator _mediator;
    private readonly ICandidateResourceTokenService _candidateTokens;
    private readonly ICandidateLinkMailer _linkMailer;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        IMediator mediator, ICandidateResourceTokenService candidateTokens, ICandidateLinkMailer linkMailer, ILogger<ConversationsController> logger)
    {
        _mediator = mediator;
        _candidateTokens = candidateTokens;
        _linkMailer = linkMailer;
        _logger = logger;
    }

    /// <summary>
    /// Anonymous, same reasoning as <see cref="CandidatesController"/>'s apply endpoint:
    /// a candidate chatting with the bot has no session, so there's no tenant claim to
    /// resolve from. Resolves it from the position instead. Issues the candidate access
    /// token here — this is the one point where a conversation goes from "doesn't exist"
    /// to "exists," so it's the only place that can hand out the capability for it.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Start(StartConversationCommand command, CancellationToken cancellationToken)
    {
        var tenantResult = await _mediator.Send(new ResolvePositionTenantQuery(command.PositionId), cancellationToken);
        if (tenantResult.IsFailure)
            return tenantResult.ToActionResult(this);

        using var _ = AmbientTenantOverride.Begin(TenantId.From(tenantResult.Value));

        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailure)
            return result.ToActionResult(this);

        var token = _candidateTokens.Issue(
            CandidateResourceType.Conversation, result.Value.ConversationId, DateTimeOffset.UtcNow.Add(CandidateTokenLifetime));

        // Best-effort: the conversation itself already exists and its token is already
        // minted by this point, so a delivery failure here must not fail an otherwise
        // successful Start — same reasoning as EvaluationSubmittedEventHandler's
        // post-save side effect. The candidate app still shows/copies the link in the
        // current session regardless (ADR 0004), so a failed email isn't a dead end.
        try
        {
            await _linkMailer.SendConversationLinkAsync(command.PositionId, command.CandidateId, result.Value.ConversationId, token, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to email conversation link for candidate {CandidateId}.", command.CandidateId);
        }

        return Ok(new StartConversationResponse(result.Value, token));
    }

    /// <summary>
    /// Panel-facing: HR re-sends the link when a candidate says they never got it, or
    /// lost it. Unlike Start's best-effort delivery, sending IS the point of this action,
    /// so a failure here surfaces as a real error rather than being swallowed.
    /// </summary>
    [HttpPost("resend-link")]
    public async Task<IActionResult> ResendLink([FromBody] ResendConversationLinkRequest request, CancellationToken cancellationToken)
    {
        var lookup = await _mediator.Send(new GetConversationForCandidateQuery(request.CandidateId, request.PositionId), cancellationToken);
        if (lookup.IsFailure)
            return lookup.ToActionResult(this);
        if (lookup.Value is not { } conversationId)
            return NotFound();

        var token = _candidateTokens.Issue(
            CandidateResourceType.Conversation, conversationId, DateTimeOffset.UtcNow.Add(CandidateTokenLifetime));

        await _linkMailer.SendConversationLinkAsync(request.PositionId, request.CandidateId, conversationId, token, cancellationToken);

        return Ok();
    }

    [AllowAnonymous]
    [RequireCandidateResourceToken(CandidateResourceType.Conversation, "conversationId")]
    [HttpPost("{conversationId:guid}/responses")]
    public async Task<IActionResult> Respond(Guid conversationId, [FromBody] RespondRequest request, CancellationToken cancellationToken)
    {
        var tenantResult = await _mediator.Send(new ResolveConversationTenantQuery(conversationId), cancellationToken);
        if (tenantResult.IsFailure)
            return tenantResult.ToActionResult(this);

        using var _ = AmbientTenantOverride.Begin(TenantId.From(tenantResult.Value));

        var result = await _mediator.Send(new ProcessUserResponseCommand(conversationId, request.Response), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{conversationId:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid conversationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetConversationHistoryQuery(conversationId), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// HR/panel-only lookup: given a candidate + position (what CandidateDetailPage
    /// already has), finds the matching conversation, if the candidate has started one.
    /// `conversationId: null` is a normal, successful "hasn't started chatting yet"
    /// answer, not a 404 — see GetConversationForCandidateQuery's remarks.
    /// </summary>
    [HttpGet("by-candidate")]
    public async Task<IActionResult> GetForCandidate([FromQuery] Guid candidateId, [FromQuery] Guid positionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetConversationForCandidateQuery(candidateId, positionId), cancellationToken);
        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(new ConversationForCandidateResponse(result.Value));
    }

    /// <summary>
    /// KVKK: an HR/panel user acting on a candidate's consent-withdrawal request,
    /// mirroring <see cref="CasesController"/>'s withdraw-consent endpoint (see
    /// docs/adr/0001 and 0003). Authenticated like the rest of this controller's
    /// non-candidate-facing routes.
    /// </summary>
    [HttpPost("{conversationId:guid}/withdraw-consent")]
    public async Task<IActionResult> WithdrawConsent(
        Guid conversationId, WithdrawConversationConsentRequest request, CancellationToken cancellationToken)
    {
        var command = new RequestConversationDataDeletionCommand(conversationId, ConversationDataDeletionReason.ConsentWithdrawn, request.Reason);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    public sealed record RespondRequest(string? Response);
}

public sealed record WithdrawConversationConsentRequest(string? Reason);

/// <summary>Wraps StartConversationResult with the candidate access token — see ADR 0004. Every later request for this conversation must present this token in the X-Candidate-Token header.</summary>
public sealed record StartConversationResponse(StartConversationResult Conversation, string CandidateToken);

public sealed record ConversationForCandidateResponse(Guid? ConversationId);

public sealed record ResendConversationLinkRequest(Guid CandidateId, Guid PositionId);
