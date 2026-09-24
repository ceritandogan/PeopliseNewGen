using MediatR;
using Peoplise.Modules.ATS.Application.Candidates.Queries;
using Peoplise.SharedKernel.Notifications;

namespace Peoplise.Api.Services;

/// <summary>
/// Composition-root orchestration for emailing a candidate their resource link (ADR
/// 0004's token-based conversation/case access) — lives in Peoplise.Api rather than a
/// module because it spans ATS (candidate contact info), HrBot/VideoInterview (the
/// resource being linked to), and Infrastructure (<see cref="IEmailSender"/>), the same
/// reasoning <c>ConversationsController.Start</c> already applies to cross-module calls.
/// Throws (mirroring <see cref="IEmailSender"/>'s own exception-based contract) rather
/// than returning a Result — callers decide for themselves whether a failure here is
/// best-effort (Start: catch, log, don't fail the response) or must-succeed (Resend link:
/// let it surface as an error, since sending IS the point of that action).
/// </summary>
public interface ICandidateLinkMailer
{
    Task SendConversationLinkAsync(Guid positionId, Guid candidateId, Guid conversationId, string candidateToken, CancellationToken cancellationToken = default);

    Task SendCaseLinkAsync(Guid positionId, Guid candidateId, Guid caseId, string candidateToken, CancellationToken cancellationToken = default);
}

public sealed class CandidateLinkMailer : ICandidateLinkMailer
{
    private readonly IMediator _mediator;
    private readonly IEmailSender _emailSender;
    private readonly string _candidateAppBaseUrl;

    public CandidateLinkMailer(IMediator mediator, IEmailSender emailSender, IConfiguration configuration)
    {
        _mediator = mediator;
        _emailSender = emailSender;
        _candidateAppBaseUrl = configuration["CandidateApp:BaseUrl"]
            ?? throw new InvalidOperationException("CandidateApp:BaseUrl is not configured.");
    }

    public Task SendConversationLinkAsync(Guid positionId, Guid candidateId, Guid conversationId, string candidateToken, CancellationToken cancellationToken = default) =>
        SendAsync(positionId, candidateId, BuildUrl("bot-chat", positionId, "conversationId", conversationId, candidateToken), cancellationToken);

    public Task SendCaseLinkAsync(Guid positionId, Guid candidateId, Guid caseId, string candidateToken, CancellationToken cancellationToken = default) =>
        SendAsync(positionId, candidateId, BuildUrl("video-interview", positionId, "caseId", caseId, candidateToken), cancellationToken);

    private async Task SendAsync(Guid positionId, Guid candidateId, string url, CancellationToken cancellationToken)
    {
        var contactResult = await _mediator.Send(new GetCandidateContactQuery(positionId, candidateId), cancellationToken);
        if (contactResult.IsFailure)
        {
            throw new InvalidOperationException($"Could not resolve candidate contact for email delivery: {contactResult.Error.Message}");
        }

        var contact = contactResult.Value;
        const string subject = "Başvurunuz için bağlantınız";
        var body = $"""
            <p>Merhaba {contact.Name},</p>
            <p>Başvuru sürecinize aşağıdaki bağlantıdan devam edebilirsiniz:</p>
            <p><a href="{url}">{url}</a></p>
            """;

        await _emailSender.SendAsync(contact.Email, subject, body, cancellationToken);
    }

    private string BuildUrl(string path, Guid positionId, string resourceKey, Guid resourceId, string candidateToken) =>
        $"{_candidateAppBaseUrl}/{path}/{positionId}?{resourceKey}={resourceId}&token={Uri.EscapeDataString(candidateToken)}";
}
