namespace AssetManagement.Application.Common.Interfaces;

/// <summary>Best-effort email delivery (pedido: "notificaciones... correo"). The implementation decides
/// whether to actually send — a no-op implementation that only logs is used when no SMTP server is
/// configured, so the app never pretends to deliver mail it cannot (see F8 plan decision 2). No retry
/// queue in V1 — a failed send is not automatically retried.</summary>
public interface IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken);
}
