using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Peoplise.Api.Services;
using Peoplise.Infrastructure.Security;
using Peoplise.Modules.ATS.Application.Candidates.Commands;
using Peoplise.Modules.ATS.Application.Candidates.Queries;
using Peoplise.Modules.ATS.Application.Positions.Queries;
using Peoplise.Modules.HrBot.Application.Conversations.Commands;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.Modules.VideoInterview.Application.Cases.Commands;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Results;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Peoplise.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/candidates")]
public sealed class CandidatesController : ControllerBase
{
    /// <summary>How long a manually-added candidate's link stays usable — same as Start's own. See ADR 0004.</summary>
    private static readonly TimeSpan CandidateTokenLifetime = TimeSpan.FromDays(30);

    private readonly IMediator _mediator;
    private readonly ICandidateResourceTokenService _candidateTokens;
    private readonly ICandidateLinkMailer _linkMailer;
    private readonly ILogger<CandidatesController> _logger;

    public CandidatesController(
        IMediator mediator, ICandidateResourceTokenService candidateTokens, ICandidateLinkMailer linkMailer, ILogger<CandidatesController> logger)
    {
        _mediator = mediator;
        _candidateTokens = candidateTokens;
        _linkMailer = linkMailer;
        _logger = logger;
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

    /// <summary>
    /// HR-facing counterpart to <see cref="Apply"/>: same underlying
    /// <see cref="SubmitCandidateApplicationCommand"/>, authenticated instead of anonymous,
    /// with <c>CandidateId</c> minted server-side rather than by a client that has no
    /// reason to know it in advance. Immediately best-effort starts a conversation and/or
    /// case for whichever project type(s) the position has configured (mirroring what a
    /// self-applying candidate's own first page-load would trigger), so HR gets a real,
    /// working link back instead of an empty record — either kind missing (or both) is a
    /// normal outcome, not an error, since neither project type is guaranteed to exist.
    /// </summary>
    [HttpPost("manual")]
    public async Task<IActionResult> AddManual(AddCandidateManuallyRequest request, CancellationToken cancellationToken)
    {
        var duplicateCheck = await _mediator.Send(new CandidateExistsForPositionQuery(request.PositionId, request.CandidateEmail), cancellationToken);
        if (duplicateCheck.IsFailure)
            return duplicateCheck.ToActionResult(this);

        if (duplicateCheck.Value)
        {
            return Result.Failure<Guid>(Error.Conflict(
                "CandidateProcess.AlreadyApplied", "This candidate has already applied to this position.")).ToActionResult(this);
        }

        var candidateId = Guid.NewGuid();
        var applyCommand = new SubmitCandidateApplicationCommand(
            candidateId, request.PositionId, request.CandidateName, request.CandidateEmail, request.CandidatePhone, request.ResumeUrl);

        var applyResult = await _mediator.Send(applyCommand, cancellationToken);
        if (applyResult.IsFailure)
            return applyResult.ToActionResult(this);

        var links = new List<CandidateInterviewLink>();

        if (await TryStartConversationAsync(request.PositionId, candidateId, cancellationToken) is { } conversationLink)
            links.Add(conversationLink);

        if (await TryStartCaseAsync(request.PositionId, candidateId, cancellationToken) is { } caseLink)
            links.Add(caseLink);

        return Ok(new AddCandidateManuallyResponse(applyResult.Value, links));
    }

    private async Task<CandidateInterviewLink?> TryStartConversationAsync(Guid positionId, Guid candidateId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new StartConversationCommand(positionId, candidateId, ConversationInterface.WebChat), cancellationToken);
        if (result.IsFailure)
            return null;

        var token = _candidateTokens.Issue(CandidateResourceType.Conversation, result.Value.ConversationId, DateTimeOffset.UtcNow.Add(CandidateTokenLifetime));
        var url = _linkMailer.BuildConversationUrl(positionId, result.Value.ConversationId, token);

        try
        {
            await _linkMailer.SendConversationLinkAsync(positionId, candidateId, result.Value.ConversationId, token, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to email conversation link for manually added candidate {CandidateId}.", candidateId);
        }

        return new CandidateInterviewLink("bot-chat", url);
    }

    private async Task<CandidateInterviewLink?> TryStartCaseAsync(Guid positionId, Guid candidateId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new StartCandidateCaseCommand(positionId, candidateId), cancellationToken);
        if (result.IsFailure)
            return null;

        var token = _candidateTokens.Issue(CandidateResourceType.Case, result.Value.CaseId, DateTimeOffset.UtcNow.Add(CandidateTokenLifetime));
        var url = _linkMailer.BuildCaseUrl(positionId, result.Value.CaseId, token);

        try
        {
            await _linkMailer.SendCaseLinkAsync(positionId, candidateId, result.Value.CaseId, token, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to email case link for manually added candidate {CandidateId}.", candidateId);
        }

        return new CandidateInterviewLink("video-interview", url);
    }

    /// <summary>No AuthorId here — deliberately: it's derived server-side from the caller's access token. See AddNote.</summary>
    public sealed record AddNoteRequest(string Text, bool IsPrivate);

    /// <summary>No EvaluatorId here — deliberately: it's derived server-side from the caller's access token, never accepted from the client. See SubmitEvaluation.</summary>
    public sealed record SubmitEvaluationRequest(decimal Score, string? Comments);

    public sealed record AddCandidateManuallyRequest(
        Guid PositionId, string CandidateName, string CandidateEmail, string? CandidatePhone, string? ResumeUrl);

    /// <summary>
    /// <c>Links</c> is empty when the position has no configured project of either kind
    /// yet — a normal "nothing to send" outcome, not an error; the panel shows a
    /// "no interview flow configured" message in that case rather than a link.
    /// </summary>
    public sealed record AddCandidateManuallyResponse(Guid CandidateProcessId, IReadOnlyList<CandidateInterviewLink> Links);

    /// <summary>Type is "bot-chat" or "video-interview" — the same path segment the link itself uses.</summary>
    public sealed record CandidateInterviewLink(string Type, string Url);
}
