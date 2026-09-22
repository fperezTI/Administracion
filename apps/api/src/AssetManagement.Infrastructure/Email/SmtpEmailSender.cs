using System.Net;
using System.Net.Mail;
using AssetManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Infrastructure.Email;

/// <summary>Real SMTP delivery, registered only when <c>Smtp:Host</c> is configured (see F8 plan decision
/// 2) — a single best-effort attempt, no retry queue. A delivery failure is logged, not thrown, so a
/// notification's in-app row (already saved) is never rolled back because email delivery failed.</summary>
public sealed class SmtpEmailSender(string host, int port, string? username, string? password, string fromAddress, ILogger<SmtpEmailSender> logger)
    : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken, bool isHtml = false)
    {
        try
        {
            using var client = new SmtpClient(host, port);
            if (!string.IsNullOrWhiteSpace(username))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            using var message = new MailMessage(fromAddress, toEmail, subject, body) { IsBodyHtml = isHtml };
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No fue posible enviar el correo a {ToEmail}", toEmail);
        }
    }
}
