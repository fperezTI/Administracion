using AssetManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Infrastructure.Email;

/// <summary>
/// The only IEmailSender registered in DI — decides which real channel to use on every single call
/// (instead of once at startup, like before) so a change made from the system settings admin screen takes
/// effect immediately: Smtp:Host (still config-only, unrelated to that screen) wins if set; otherwise
/// Microsoft Graph if SystemSettingsProvider resolves a full set of credentials + sender mailbox;
/// otherwise the same "not configured" no-op as before.
/// </summary>
public sealed class ConfigurableEmailSender(
    ISystemSettingsProvider settingsProvider, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILoggerFactory loggerFactory)
    : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken, bool isHtml = false)
    {
        var smtpHost = configuration["Smtp:Host"];
        if (!string.IsNullOrWhiteSpace(smtpHost))
        {
            var smtpPort = configuration.GetValue("Smtp:Port", 25);
            var smtpUsername = configuration["Smtp:Username"];
            var smtpPassword = configuration["Smtp:Password"];
            var smtpFromAddress = configuration["Smtp:FromAddress"] ?? "no-reply@example.com";
            var smtpSender = new SmtpEmailSender(
                smtpHost, smtpPort, smtpUsername, smtpPassword, smtpFromAddress, loggerFactory.CreateLogger<SmtpEmailSender>());
            await smtpSender.SendAsync(toEmail, subject, body, cancellationToken, isHtml);
            return;
        }

        var graph = await settingsProvider.GetGraphSettingsAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(graph.TenantId) && !string.IsNullOrWhiteSpace(graph.ClientId)
            && !string.IsNullOrWhiteSpace(graph.ClientSecret) && !string.IsNullOrWhiteSpace(graph.SenderMailbox))
        {
            var graphSender = new GraphEmailSender(
                httpClientFactory.CreateClient("GraphMail"), graph.TenantId, graph.ClientId, graph.ClientSecret,
                graph.SenderMailbox, loggerFactory.CreateLogger<GraphEmailSender>());
            await graphSender.SendAsync(toEmail, subject, body, cancellationToken, isHtml);
            return;
        }

        var noOpSender = new NoOpEmailSender(loggerFactory.CreateLogger<NoOpEmailSender>());
        await noOpSender.SendAsync(toEmail, subject, body, cancellationToken, isHtml);
    }
}
