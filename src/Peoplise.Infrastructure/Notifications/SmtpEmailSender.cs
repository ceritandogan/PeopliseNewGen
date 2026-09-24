using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Peoplise.SharedKernel.Notifications;

namespace Peoplise.Infrastructure.Notifications;

/// <summary>
/// The real <see cref="IEmailSender"/>, backed by plain SMTP via MailKit — chosen
/// over a provider-specific SDK (SendGrid, Mailgun, SES) precisely because it's
/// provider-agnostic: any SMTP endpoint works, including a sandbox like Mailtrap
/// for local/dev verification, with zero code change to swap providers later. Only
/// registered once <c>Email:Smtp:Host</c> is configured; see
/// <c>scripts/setup-email-smtp.sh</c> and <c>DependencyInjection.AddNotifications</c>.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;

    public SmtpEmailSender(SmtpOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(string toAddress, string subject, string bodyHtml, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = bodyHtml }.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = _options.UseSsl ? SecureSocketOptions.Auto : SecureSocketOptions.StartTlsWhenAvailable;
        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);
        if (!string.IsNullOrEmpty(_options.Username))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        }
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

public sealed record SmtpOptions(string Host, int Port, string Username, string Password, string FromAddress, string FromName, bool UseSsl);
