using AssetManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Infrastructure.Email;

/// <summary>Used when no <c>Smtp:Host</c> is configured — never pretends to deliver mail it can't (see F8
/// plan decision 2): the notification's in-app row is always the real channel, this just makes the gap
/// visible in the log instead of silently swallowing it.</summary>
public sealed class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "SMTP no configurado — no se envió el correo '{Subject}' a {ToEmail} (la notificación en la app sí se creó).",
            subject, toEmail);
        return Task.CompletedTask;
    }
}
