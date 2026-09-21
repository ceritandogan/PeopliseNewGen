using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Peoplise.Api.Filters;
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

    public ConversationsController(IMediator mediator, ICandidateResourceTokenService candidateTokens)
    {
        _mediator = mediator;
        _candidateTokens = candidateTokens;
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

        return Ok(new StartConversationResponse(result.Value, token));
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
