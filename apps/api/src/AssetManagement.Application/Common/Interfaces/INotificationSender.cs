namespace AssetManagement.Application.Common.Interfaces;

/// <summary>Port anticipated since F0 (pedido §4) — writes the in-app notification (the channel that
/// always works) and best-effort forwards it by email via <see cref="IEmailSender"/>.</summary>
public interface INotificationSender
{
    public Task NotifyAsync(
        Guid userId, string type, string title, string body, Guid? companyId, CancellationToken cancellationToken,
        string? emailBodyHtml = null);
}
