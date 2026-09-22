using System.Net.Http.Headers;
using System.Text.Json;
using AssetManagement.Application.Common.Interfaces;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Infrastructure.Email;

/// <summary>
/// Real email delivery via the Microsoft Graph REST API (`POST /users/{mailbox}/sendMail`), reusing the
/// same app-only credential and REST-over-SDK approach as
/// <see cref="AssetManagement.Infrastructure.Directory.GraphDirectoryUserSearch"/> — registered only when
/// <c>MicrosoftGraph:SenderMailbox</c> is configured (and no <c>Smtp:Host</c> takes precedence, see
/// DependencyInjection.cs). Requires the Graph *application* permission `Mail.Send` with admin consent on
/// the same App Registration used for directory search — see docs/security/entra-id-setup.md.
/// </summary>
public sealed class GraphEmailSender(HttpClient httpClient, ClientSecretCredential credential, string senderMailbox, ILogger<GraphEmailSender> logger)
    : IEmailSender
{
    private static readonly string[] GraphScopes = ["https://graph.microsoft.com/.default"];

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken, bool isHtml = false)
    {
        try
        {
            var token = await credential.GetTokenAsync(new TokenRequestContext(GraphScopes), cancellationToken);

            var payload = new
            {
                message = new
                {
                    subject,
                    body = new { contentType = isHtml ? "HTML" : "Text", content = body },
                    toRecipients = new[] { new { emailAddress = new { address = toEmail } } },
                },
                saveToSentItems = false,
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(senderMailbox)}/sendMail")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "No fue posible enviar el correo a {ToEmail} vía Microsoft Graph ({StatusCode}): {Body}",
                    toEmail, response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No fue posible enviar el correo a {ToEmail} vía Microsoft Graph", toEmail);
        }
    }
}
