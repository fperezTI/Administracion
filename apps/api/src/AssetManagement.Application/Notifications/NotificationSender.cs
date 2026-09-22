using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Notifications;

/// <summary>
/// Needs nothing Infrastructure-specific beyond <see cref="IEmailSender"/> (itself a thin port), so it
/// lives entirely in Application — same reasoning as <c>ApprovalCoordinator</c> (F4).
/// </summary>
public sealed class NotificationSender(IApplicationDbContext db, IEmailSender emailSender, IClock clock) : INotificationSender
{
    public async Task NotifyAsync(
        Guid userId, string type, string title, string body, Guid? companyId, CancellationToken cancellationToken,
        string? emailBodyHtml = null)
    {
        var notification = Notification.Create(userId, type, title, body, companyId, clock.UtcNow);
        db.Notifications.Add(notification);

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is not null)
        {
            // Best-effort, not part of the notification's own transaction outcome — see F8 plan decision 2.
            await emailSender.SendAsync(
                user.Email, title, emailBodyHtml ?? body, cancellationToken, isHtml: emailBodyHtml is not null);
        }
    }
}
