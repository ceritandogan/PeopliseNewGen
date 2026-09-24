using Peoplise.SharedKernel.Notifications;

namespace Peoplise.Infrastructure.Notifications;

/// <summary>
/// The default <see cref="IEmailSender"/> registration: fails loudly and immediately
/// rather than silently pretending an email was sent. Replace this registration (see
/// <c>DependencyInjection.AddNotifications</c>) by configuring the <c>Email:Smtp:*</c>
/// settings — run <c>scripts/setup-email-smtp.sh</c> for a guided local setup — before
/// using any feature that sends email (e.g. candidate link delivery).
/// </summary>
public sealed class NotConfiguredEmailSender : IEmailSender
{
    public Task SendAsync(string toAddress, string subject, string bodyHtml, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "No SMTP email sender is configured. Set Email:Smtp:Host/Port/Username/Password/FromAddress/FromName "
            + "(run scripts/setup-email-smtp.sh for a guided local setup) before using any email-dependent feature.");
}
