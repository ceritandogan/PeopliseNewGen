namespace Peoplise.SharedKernel.Notifications;

/// <summary>
/// Outbound email abstraction, mirroring <c>IAIProvider</c>'s "swappable external
/// provider" shape — provider-agnostic (SMTP today, could be a transactional API
/// later) and exception-based rather than <c>Result</c>-based, for the same reason
/// <c>IAIProvider</c> is: it's an external-system call, not a domain operation.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toAddress, string subject, string bodyHtml, CancellationToken cancellationToken = default);
}
